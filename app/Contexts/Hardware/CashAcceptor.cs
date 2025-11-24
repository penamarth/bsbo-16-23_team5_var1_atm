using ATM.Kernel.Common;

namespace ATM.Contexts.Hardware;

public class CashAcceptor {
    private decimal _totalAmount;

    public decimal AcceptCash() {
        // Simulate accepting cash
        Logger.Log("Вставьте купюры...");
        // In a real ATM, this would interact with hardware.
        // Here we simulate it by asking user to type amount.
        Console.Write("Введите сумму внесенных средств: ");
        string? input = Console.ReadLine();
        if (decimal.TryParse(input, out decimal amount) && amount > 0) {
            _totalAmount += amount;
            Logger.Log($"Принято: {amount:C}");
            return amount;
        }
        Logger.Log("Ошибка внесения средств или отмена.");
        return 0m;
    }

    public void EjectCash() {
        Logger.Log("Возврат внесенных средств...");
        _totalAmount = 0; // Reset for simulation
    }
    
    // For Collector
    public decimal CollectAllCash() {
        decimal amount = _totalAmount;
        _totalAmount = 0;
        Logger.Log($"Инкассация: изъято {amount:C} из купюроприемника.");
        return amount;
    }
}
