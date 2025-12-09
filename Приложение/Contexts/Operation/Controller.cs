using ATM.Kernel.Common;
using ATM.Kernel.Hardware;
using ATM.Kernel.Models;
using ATM.Contexts.Banking;
using ATM.Kernel.Storage;

namespace ATM.Contexts.Operation;

public class ATMController {
    private readonly CardReader _cardReader;
    private readonly CashDispenser _cashDispenser;
    private readonly CashAcceptor _cashAcceptor;
    private readonly IBankingService _bankingService;
    private readonly ReceiptPrinter _receiptPrinter;
    private readonly OperationJournal _operationJournal;

    private AtmSession? _currentSession;

    internal ATMController(CardReader cardReader, CashDispenser cashDispenser, CashAcceptor cashAcceptor, IBankingService bankingService, ReceiptPrinter receiptPrinter, OperationJournal operationJournal) {
        _cardReader = cardReader;
        _cashDispenser = cashDispenser;
        _cashAcceptor = cashAcceptor;
        _bankingService = bankingService;
        _receiptPrinter = receiptPrinter;
        _operationJournal = operationJournal;
    }
    
    public void Setup() {
        Logger.Log("Инициализация контроллера банкомата...");
    }

    public AtmSession? StartSession(CardData cardData, Pin pin)
    {
        var maskedCard = OperationEntry.MaskCard(cardData.CardNumber);
        Logger.Log($"Запуск сессии для карты {maskedCard}...");
        var auth = _bankingService.Authenticate(cardData, pin);
        if (!(auth.IsAuthenticated && auth.AccountId.HasValue))
        {
            _operationJournal.Add(OperationEntry.Create("AUTH", OperationStatus.Failed, cardData.CardNumber, null, null, "Неверный PIN"));
            Logger.Log("Сессия не создана: аутентификация не пройдена.", LogLevel.Warning);
            return null;
        }

        var session = new AtmSession(auth.AccountId.Value, cardData, pin);
        _currentSession = session;
        _operationJournal.Add(OperationEntry.Create("AUTH", OperationStatus.Success, cardData.CardNumber, auth.AccountId.Value.Value, null, "Аутентификация успешна"));
        Logger.Log($"Сессия создана для карты {maskedCard}.");
        return session;
    }

    public decimal CheckBalance(AtmSession session)
    {
        Logger.Log("Запрос баланса (контроллер)...");
        var balance = _bankingService.GetBalance(session.AccountId);
        _operationJournal.Add(OperationEntry.Create("CHECK_BALANCE", OperationStatus.Success, session.Card.CardNumber, session.AccountId.Value, balance, "Баланс получен"));
        Logger.Log($"Баланс карты: {balance:C}");
        return balance;
    }

    public bool Withdraw(AtmSession session, decimal amount)
    {
        Logger.Log($"Запрос на снятие {amount:C}...");
        if (!_cashDispenser.CanDispense(amount))
        {
            _operationJournal.Add(OperationEntry.Create("WITHDRAWAL", OperationStatus.Failed, session.Card.CardNumber, session.AccountId.Value, amount, "В банкомате нет нужной суммы"));
            Logger.Log("Снятие отклонено: недостаточно наличности в банкомате.", LogLevel.Warning);
            return false;
        }

        if (!_bankingService.ExecuteWithdrawal(session.AccountId, amount))
        {
            _operationJournal.Add(OperationEntry.Create("WITHDRAWAL", OperationStatus.Failed, session.Card.CardNumber, session.AccountId.Value, amount, "Отклонено банковской системой"));
            Logger.Log("Снятие отклонено банковской системой.", LogLevel.Warning);
            return false;
        }

        if (_cashDispenser.TryDispense(amount))
        {
            _operationJournal.Add(OperationEntry.Create("WITHDRAWAL", OperationStatus.Success, session.Card.CardNumber, session.AccountId.Value, amount, $"Остаток банкомата: {_cashDispenser.Remaining:C}"));
            Logger.Log($"Снятие выполнено: {amount:C}. Остаток банкомата: {_cashDispenser.Remaining:C}");
            return true;
        }

        _operationJournal.Add(OperationEntry.Create("WITHDRAWAL", OperationStatus.Failed, session.Card.CardNumber, session.AccountId.Value, amount, "Ошибка диспенсера"));
        Logger.Log("Снятие не выполнено: ошибка диспенсера.", LogLevel.Warning);
        return false;
    }

    public bool Deposit(AtmSession session, decimal amount, bool cashAlreadyAccepted = false)
    {
        Logger.Log($"Запрос на внесение {amount:C}...");
        if (amount <= 0)
        {
            _operationJournal.Add(OperationEntry.Create("DEPOSIT", OperationStatus.Failed, session.Card.CardNumber, session.AccountId.Value, amount, "Сумма некорректна"));
            Logger.Log("Внесение отклонено: сумма некорректна.", LogLevel.Warning);
            return false;
        }

        if (!cashAlreadyAccepted)
        {
            _cashAcceptor.AcceptCash(amount);
        }

        if (_bankingService.Deposit(session.AccountId, amount))
        {
            _operationJournal.Add(OperationEntry.Create("DEPOSIT", OperationStatus.Success, session.Card.CardNumber, session.AccountId.Value, amount, "Средства зачислены"));
            Logger.Log($"Внесение выполнено: {amount:C}");
            return true;
        }

        _cashAcceptor.EjectCash();
        _operationJournal.Add(OperationEntry.Create("DEPOSIT", OperationStatus.Failed, session.Card.CardNumber, session.AccountId.Value, amount, "Ошибка зачисления"));
        Logger.Log("Внесение отменено: ошибка зачисления, средства возвращены.", LogLevel.Warning);
        return false;
    }

    public bool Transfer(AtmSession session, string toCardNumber, decimal amount)
    {
        Logger.Log($"Запрос на перевод {amount:C} на карту {toCardNumber}...");
        if (amount <= 0)
        {
            _operationJournal.Add(OperationEntry.Create("TRANSFER", OperationStatus.Failed, session.Card.CardNumber, session.AccountId.Value, amount, "Некорректная сумма"));
            Logger.Log("Перевод отклонен: некорректная сумма.", LogLevel.Warning);
            return false;
        }

        var ok = _bankingService.Transfer(session.AccountId, toCardNumber, amount);
        _operationJournal.Add(OperationEntry.Create(
            "TRANSFER",
            ok ? OperationStatus.Success : OperationStatus.Failed,
            session.Card.CardNumber,
            session.AccountId.Value,
            amount,
            ok ? $"Перевод на {toCardNumber}" : "Отклонено (недостаточно средств)")
        );
        if (!ok) Logger.Log("Перевод отклонен (недостаточно средств).", LogLevel.Warning);
        else Logger.Log($"Перевод выполнен на карту {toCardNumber} на сумму {amount:C}");
        return ok;
    }

    public bool ChangePin(CardData cardData, Pin oldPin, Pin newPin)
    {
        Logger.Log("Запрос на смену PIN...");
        var ok = _bankingService.ChangePin(cardData, oldPin, newPin);
        _operationJournal.Add(OperationEntry.Create("CHANGE_PIN", ok ? OperationStatus.Success : OperationStatus.Failed, cardData.CardNumber, _currentSession?.AccountId.Value, null, ok ? "PIN изменен" : "Смена PIN отклонена"));
        Logger.Log(ok ? "PIN успешно изменен." : "Смена PIN отклонена.", ok ? LogLevel.Info : LogLevel.Warning);
        return ok;
    }

    public IReadOnlyList<OperationEntry> GetJournal() => _operationJournal.Entries;

    public void PrintJournal()
    {
        Logger.Log("=== ЖУРНАЛ ОПЕРАЦИЙ ===");
        foreach (var entry in _operationJournal.Entries)
        {
            Logger.Log($"{entry.Timestamp:HH:mm:ss} {entry.Operation} {entry.Status} {entry.Amount?.ToString("C") ?? "-"} {entry.CardNumberMasked} {entry.Details}");
        }
    }

    public void Shutdown() {
        Logger.Log("Завершение работы банкомата...", LogLevel.Warning);
    }

    public void EndSession(string? cardNumber = null)
    {
        Logger.Log($"Сессия для карты {cardNumber ?? "unknown"} завершена.");
        _operationJournal.Add(OperationEntry.Create("SESSION_END", OperationStatus.Success, cardNumber, _currentSession?.AccountId.Value, null, "Сессия завершена"));
        _currentSession = null;
    }
}