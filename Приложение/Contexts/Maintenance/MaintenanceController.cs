using ATM.Kernel.Common;
using ATM.Contexts.Hardware;
using ATM.Kernel.UI;

namespace ATM.Contexts.Maintenance;

public class MaintenanceController {
    private readonly Screen _screen;
    private readonly Keypad _keypad;
    private readonly CashDispenser _cashDispenser;
    private readonly CashAcceptor _cashAcceptor;

    private const string ServiceCode = "9999";

    public MaintenanceController(Screen screen, Keypad keypad, CashDispenser cashDispenser, CashAcceptor cashAcceptor) {
        _screen = screen;
        _keypad = keypad;
        _cashDispenser = cashDispenser;
        _cashAcceptor = cashAcceptor;
    }

    public bool AuthenticateCollector() {
        _screen.DisplayMessage("=== РЕЖИМ ОБСЛУЖИВАНИЯ ===");
        _screen.DisplayMessage("Введите сервисный код:");
        string? code = _keypad.GetInput();
        
        if (code == ServiceCode) {
            Logger.Log("Инкассатор успешно аутентифицирован.");
            return true;
        }
        
        Logger.Log("Ошибка аутентификации инкассатора.", LogLevel.Warning);
        _screen.DisplayMessage("Неверный код. Доступ запрещен.");
        return false;
    }

    public void Run() {
        if (!AuthenticateCollector()) {
            return;
        }

        bool sessionActive = true;
        while (sessionActive) {
            _screen.DisplayMessage("\n=== МЕНЮ ОБСЛУЖИВАНИЯ ===");
            _screen.DisplayMessage("1. Пополнить диспенсер");
            _screen.DisplayMessage("2. Инкассировать купюроприемник");
            _screen.DisplayMessage("3. Проверить состояние");
            _screen.DisplayMessage("4. Завершить обслуживание");

            string? choice = _keypad.GetInput();

            switch (choice) { // TODO: enum
                case "1":
                    RefillDispenser();
                    break;
                case "2":
                    CollectAcceptor();
                    break;
                case "3":
                    CheckStatus();
                    break;
                case "4":
                    sessionActive = false;
                    _screen.DisplayMessage("Обслуживание завершено.");
                    Logger.Log("Сессия обслуживания завершена.");
                    break;
                default:
                    _screen.DisplayMessage("Неверный выбор.");
                    break;
            }
        }
    }

    private void RefillDispenser() {
        _screen.DisplayMessage("Введите сумму для загрузки в диспенсер:");
        string? amountStr = _keypad.GetInput();
        
        if (decimal.TryParse(amountStr, out decimal amount) && amount > 0) {
            _cashDispenser.Refill(amount);
            _screen.DisplayMessage($"Диспенсер пополнен на {amount:C}");
            _screen.DisplayMessage($"Текущий остаток: {_cashDispenser.Remaining:C}");
        } else {
            _screen.DisplayMessage("Некорректная сумма.");
        }
    }

    private void CollectAcceptor() {
        decimal collected = _cashAcceptor.CollectAllCash();
        _screen.DisplayMessage($"Изъято из купюроприемника: {collected:C}");
    }

    private void CheckStatus() {
        _screen.DisplayMessage("=== СТАТУС УСТРОЙСТВ ===");
        _screen.DisplayMessage($"Диспенсер: {_cashDispenser.Remaining:C}");
        _screen.DisplayMessage("Купюроприемник: готов к работе");
        _screen.DisplayMessage("Принтер: готов к работе");
        _screen.DisplayMessage("Кардридер: готов к работе");
    }
}
