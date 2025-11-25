using ATM.Kernel.Common;
using ATM.Kernel.Hardware;
using ATM.Kernel.Models;
using ATM.Contexts.Banking;
using ATM.Kernel.UI;

namespace ATM.Contexts.Operation;

public class ATMController {
    private readonly Screen _screen;
    private readonly Keypad _keypad;
    private readonly CardReader _cardReader;
    private readonly CashDispenser _cashDispenser;
    private readonly CashAcceptor _cashAcceptor;
    private readonly IBankingService _bankingService;
    private readonly MenuHandler _menuHandler;
    private readonly ReceiptPrinter _receiptPrinter;

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromSeconds(60);
    private const int MaxPinAttempts = 3;

    internal ATMController(Screen screen, Keypad keypad, CardReader cardReader, CashDispenser cashDispenser, CashAcceptor cashAcceptor, IBankingService bankingService, MenuHandler menuHandler, ReceiptPrinter receiptPrinter) {
        _screen = screen;
        _keypad = keypad;
        _cardReader = cardReader;
        _cashDispenser = cashDispenser;
        _cashAcceptor = cashAcceptor;
        _bankingService = bankingService;
        _menuHandler = menuHandler;
        _receiptPrinter = receiptPrinter;
    }
    
    public void Setup() {
        Logger.Log("Инициализация контроллера банкомата...");
        _screen.DisplayMessage("Система готова к работе.");
    }
    
    public void Run() {
        Logger.Log("Банкомат перешел в режим ожидания.");
        while (true) {
            _screen.DisplayMessage("\n------------------------------------");
            _screen.DisplayMessage("Добро пожаловать! Пожалуйста, вставьте карту.");
            CardData cardData = _cardReader.ReadCard();
            ProcessClientSession(cardData);
        }
    }
    
    public void Shutdown() {
        Logger.Log("Завершение работы банкомата...", LogLevel.Warning);
    }
    
    private void ProcessClientSession(CardData cardData) {
        Logger.Log($"Начата сессия для карты {cardData.CardNumber}.");
        _screen.DisplayMessage($"Карта: {cardData.CardNumber}");

        AccountId? accountId = null;
        bool isAuthenticated = false;
        Pin? currentPin = null;
        
        for (int attempt = 1; attempt <= MaxPinAttempts; attempt++)
        {
            _screen.DisplayMessage($"Введите PIN (попытка {attempt} из {MaxPinAttempts}):");
            var pin = _keypad.GetPinInput(SessionTimeout);
            if (pin is null)
            {
                _screen.DisplayMessage("Таймаут ввода PIN. Сессия завершена.");
                EndSession(cardData);
                return;
            }

            var auth = _bankingService.Authenticate(cardData, pin.Value);
            isAuthenticated = auth.IsAuthenticated;
            accountId = auth.AccountId;
            if (isAuthenticated && accountId.HasValue) {
                currentPin = pin.Value;
                break;
            }
            _screen.DisplayMessage("Неверный PIN.");
        }

        if (!(isAuthenticated && accountId.HasValue))
        {
            _screen.DisplayMessage("PIN попытки исчерпаны. Карта заблокирована/изъята.");
            Logger.Log($"Ошибка аутентификации для карты {cardData.CardNumber}.", LogLevel.Warning);
            EndSession(cardData);
            return;
        }

        Logger.Log($"Успешная аутентификация для счета: {accountId.Value.Value}");
        bool sessionActive = true;
        while (sessionActive) {
            UserAction action = _menuHandler.GetUserActionChoice();

            switch (action) {
                case UserAction.CheckBalance:
                    var balance = _bankingService.GetBalance(accountId.Value);
                    _screen.DisplayMessage($"Текущий баланс: {balance:C}");
                    AskAndMaybePrintReceipt($"Balance: {balance:C}");
                    break;
                    
                case UserAction.Withdraw:
                    HandleWithdraw(accountId.Value);
                    break;

                case UserAction.ChangePin:
                    HandleChangePin(cardData, currentPin!.Value);
                    break;

                case UserAction.Deposit:
                    HandleDeposit(accountId.Value);
                    break;

                case UserAction.Transfer:
                    HandleTransfer(accountId.Value);
                    break;

                case UserAction.Exit:
                    sessionActive = false;
                    break;

                case UserAction.Unknown:
                default:
                    _screen.DisplayMessage("Неверный выбор. Попробуйте еще раз.");
                    break;
            }
        }

        EndSession(cardData);
    }

    private void HandleWithdraw(AccountId accountId) {
        _screen.DisplayMessage("Введите сумму для снятия:");
        string? amountStr = _keypad.GetInput(SessionTimeout);
        if (amountStr is null) {
            _screen.DisplayMessage("Таймаут ввода суммы. Возврат в меню.");
            return;
        }
        if (decimal.TryParse(amountStr, out decimal amount)) {
            if (!_cashDispenser.CanDispense(amount)) {
                _screen.DisplayMessage("Недостаточно наличности в банкомате.");
                return;
            }
            if(_bankingService.ExecuteWithdrawal(accountId, amount)) {
                if (_cashDispenser.TryDispense(amount)) {
                    AskAndMaybePrintReceipt($"Withdrawn: {amount:C}; Remaining ATM cash: {_cashDispenser.Remaining:C}");
                }
            } else {
                _screen.DisplayMessage("Операция отклонена (баланс/лимит).");
            }
        } else {
            _screen.DisplayMessage("Неверный формат суммы.");
        }
    }

    private void HandleChangePin(CardData cardData, Pin oldPin) {
        _screen.DisplayMessage("Введите новый PIN:");
        var newPin = _keypad.GetPinInput(SessionTimeout);
        if (newPin is null) {
            _screen.DisplayMessage("Таймаут ввода. Возврат в меню.");
            return;
        }
        _screen.DisplayMessage("Подтвердите новый PIN:");
        var confirmPin = _keypad.GetPinInput(SessionTimeout);
        if (confirmPin is null) {
            _screen.DisplayMessage("Таймаут ввода. Возврат в меню.");
            return;
        }
        if (newPin.Value.Value != confirmPin.Value.Value) {
            _screen.DisplayMessage("PIN-коды не совпадают. Операция отменена.");
            return;
        }
        if (_bankingService.ChangePin(cardData, oldPin, newPin.Value)) {
            _screen.DisplayMessage("PIN успешно изменен!");
        } else {
            _screen.DisplayMessage("Ошибка при смене PIN.");
        }
    }

    private void HandleDeposit(AccountId accountId) {
        _screen.DisplayMessage("Внесение наличных. Вставьте купюры.");
        decimal amount = _cashAcceptor.AcceptCash();
        if (amount <= 0) {
            _screen.DisplayMessage("Операция отменена.");
            return;
        }
        _screen.DisplayMessage($"Принято: {amount:C}. Подтвердить? (y/n)");
        var confirm = _keypad.GetInput(SessionTimeout);
        if (string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase)) {
            if (_bankingService.Deposit(accountId, amount)) {
                _screen.DisplayMessage($"Средства успешно зачислены: {amount:C}");
                AskAndMaybePrintReceipt($"Deposited: {amount:C}");
            } else {
                _screen.DisplayMessage("Ошибка зачисления.");
                _cashAcceptor.EjectCash();
            }
        } else {
            _screen.DisplayMessage("Операция отменена. Возврат средств.");
            _cashAcceptor.EjectCash();
        }
    }

    private void HandleTransfer(AccountId accountId) {
        _screen.DisplayMessage("Введите номер карты получателя:");
        var toCard = _keypad.GetInput(SessionTimeout);
        if (string.IsNullOrWhiteSpace(toCard)) {
            _screen.DisplayMessage("Некорректный номер карты.");
            return;
        }
        _screen.DisplayMessage("Введите сумму перевода:");
        string? amountStr = _keypad.GetInput(SessionTimeout);
        if (amountStr is null || !decimal.TryParse(amountStr, out decimal amount) || amount <= 0) {
            _screen.DisplayMessage("Некорректная сумма.");
            return;
        }
        _screen.DisplayMessage($"Перевод {amount:C} на карту {toCard}. Подтвердить? (y/n)");
        var confirm = _keypad.GetInput(SessionTimeout);
        if (string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase)) {
            if (_bankingService.Transfer(accountId, toCard, amount)) {
                _screen.DisplayMessage($"Перевод выполнен: {amount:C} на карту {toCard}");
                AskAndMaybePrintReceipt($"Transfer: {amount:C} to {toCard}");
            } else {
                _screen.DisplayMessage("Операция отклонена (недостаточно средств).");
            }
        } else {
            _screen.DisplayMessage("Операция отменена.");
        }
    }

    private void AskAndMaybePrintReceipt(string text)
    {
        _screen.DisplayMessage("Печать чека? (y/n)");
        var answer = _keypad.GetInput(SessionTimeout);
        if (string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
        {
            _receiptPrinter.PrintReceipt(text);
        }
    }

    private void EndSession(CardData cardData)
    {
        _screen.DisplayMessage("Сессия завершена. Заберите карту.");
        Logger.Log($"Сессия для карты {cardData.CardNumber} завершена.");
    }
}