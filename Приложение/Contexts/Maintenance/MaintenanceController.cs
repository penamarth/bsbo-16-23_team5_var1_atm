using ATM.Kernel.Common;
using ATM.Kernel.Hardware;

namespace ATM.Contexts.Maintenance;

public class MaintenanceController {
    private readonly CashDispenser _cashDispenser;
    private readonly CashAcceptor _cashAcceptor;

    private const string ServiceCode = "9999";

    public MaintenanceController(CashDispenser cashDispenser, CashAcceptor cashAcceptor) {
        _cashDispenser = cashDispenser;
        _cashAcceptor = cashAcceptor;
    }

    public bool AuthenticateCollector(string code) {
        if (code == ServiceCode) {
            Logger.Log("Инкассатор успешно аутентифицирован.");
            return true;
        }
        
        Logger.Log("Ошибка аутентификации инкассатора.", LogLevel.Warning);
        return false;
    }

    public void RefillDispenser(decimal amount) {
        if (amount <= 0) {
            Logger.Log("Некорректная сумма для пополнения.", LogLevel.Warning);
            return;
        }
        _cashDispenser.Refill(amount);
        Logger.Log($"Диспенсер пополнен на {amount:C}. Текущий остаток: {_cashDispenser.Remaining:C}");
    }

    public decimal CollectAcceptor() {
        decimal collected = _cashAcceptor.CollectAllCash();
        Logger.Log($"Инкассация: изъято {collected:C}");
        return collected;
    }

    public void CheckStatus() {
        Logger.Log("=== СТАТУС УСТРОЙСТВ ===");
        Logger.Log($"Диспенсер: {_cashDispenser.Remaining:C}");
        Logger.Log("Купюроприемник: готов к работе");
        Logger.Log("Принтер: готов к работе");
        Logger.Log("Кардридер: готов к работе");
    }
}
