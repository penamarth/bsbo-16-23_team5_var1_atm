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

    internal ATMController(Screen screen, Keypad keypad, CardReader cardReader, CashDispenser cashDispenser, IBankingService bankingService, MenuHandler menuHandler) {
        _screen = screen;
        _keypad = keypad;
        _cardReader = cardReader;
        _cashDispenser = cashDispenser;
        _bankingService = bankingService;
        _menuHandler = menuHandler;
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
        Pin pin = _keypad.GetPinInput();

        var (isAuthenticated, accountId) = _bankingService.Authenticate(cardData, pin);

        if (isAuthenticated && accountId.HasValue) {
            Logger.Log($"Успешная аутентификация для счета: {accountId.Value.Value}");
            
            bool sessionActive = true;
            while (sessionActive) {
                UserAction action = _menuHandler.GetUserActionChoice();

                switch (action) {
                    case UserAction.CheckBalance:
                        var balance = _bankingService.GetBalance(accountId.Value);
                        _screen.DisplayMessage($"Текущий баланс: {balance:C}");
                        break;
                        
                    case UserAction.Withdraw:
                        _screen.DisplayMessage("Введите сумму для снятия:");
                        string amountStr = _keypad.GetInput();
                        if (decimal.TryParse(amountStr, out decimal amount)) {
                            if(_bankingService.ExecuteWithdrawal(accountId.Value, amount)) {
                                _cashDispenser.DispenseCash(amount);
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
        } else {
            Logger.Log($"Ошибка аутентификации для карты {cardData.CardNumber}.", LogLevel.Warning);
            _screen.DisplayMessage("Ошибка авторизации.");
        }

        _screen.DisplayMessage("Сессия завершена. Заберите карту.");
        Logger.Log($"Сессия для карты {cardData.CardNumber} завершена.");
    }
}