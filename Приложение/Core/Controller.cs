using ATM.Common;
using ATM.Hardware;
using ATM.Models;
using ATM.Services.Api;
using ATM.UI;

namespace ATM.Core;

public class ATMController {
    private readonly Screen _screen;
    private readonly Keypad _keypad;
    private readonly CardReader _cardReader;
    private readonly CashDispenser _cashDispenser;
    private readonly IBankingService _bankingService;
    private readonly MenuHandler _menuHandler;
    private readonly ReceiptPrinter _receiptPrinter;

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromSeconds(60);
    private const int MaxPinAttempts = 3;

    internal ATMController(Screen screen, Keypad keypad, CardReader cardReader, CashDispenser cashDispenser, IBankingService bankingService, MenuHandler menuHandler, ReceiptPrinter receiptPrinter) {
        _screen = screen;
        _keypad = keypad;
        _cardReader = cardReader;
        _cashDispenser = cashDispenser;
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
            if (isAuthenticated && accountId.HasValue) break;
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
                    _screen.DisplayMessage("Введите сумму для снятия:");
                    string? amountStr = _keypad.GetInput(SessionTimeout);
                    if (amountStr is null) {
                        _screen.DisplayMessage("Таймаут ввода суммы. Возврат в меню.");
                        break;
                    }
                    if (decimal.TryParse(amountStr, out decimal amount)) {
                        if (!_cashDispenser.CanDispense(amount)) {
                            _screen.DisplayMessage("Недостаточно наличности в банкомате.");
                            break;
                        }
                        if(_bankingService.ExecuteWithdrawal(accountId.Value, amount)) {
                            if (_cashDispenser.TryDispense(amount)) {
                                AskAndMaybePrintReceipt($"Withdrawn: {amount:C}; Remaining ATM cash: {_cashDispenser.Remaining:C}");
                            }
                        } else {
                            _screen.DisplayMessage("Операция отклонена (баланс/лимит).");
                        }
                    } else {
                        _screen.DisplayMessage("Неверный формат суммы.");
                    }
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