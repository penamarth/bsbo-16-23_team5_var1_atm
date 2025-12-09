using ATM.Kernel.Common;

namespace ATM.Kernel.Hardware;

public class CashAcceptor {
    private decimal _totalAmount;

    public decimal AcceptCash(decimal predefinedAmount)
    {
        return AcceptCashInternal(predefinedAmount.ToString());
    }

    private decimal AcceptCashInternal(string? amountRaw)
    {
        if (decimal.TryParse(amountRaw, out decimal amount) && amount > 0) {
            _totalAmount += amount;
            Logger.Log($"Принято: {amount:C}");
            return amount;
        }
        Logger.Log("Ошибка внесения средств или отмена.");
        return 0m;
    }

    public void EjectCash() {
        Logger.Log("Возврат внесенных средств...");
        _totalAmount = 0;
    }
    
    public decimal CollectAllCash() {
        decimal amount = _totalAmount;
        _totalAmount = 0;
        Logger.Log($"Инкассация: изъято {amount:C} из купюроприемника.");
        return amount;
    }
}

