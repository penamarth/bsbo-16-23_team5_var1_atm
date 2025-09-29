using ATM.Common;

namespace ATM.Hardware;

public class CashDispenser { 
    private decimal _remaining;

    public CashDispenser(decimal initialCash = 0m)
    {
        _remaining = initialCash;
    }

    public decimal Remaining => _remaining;

    public bool CanDispense(decimal amount) => amount > 0 && amount <= _remaining;

    public bool TryDispense(decimal amount)
    {
        if (!CanDispense(amount))
        {
            Logger.Log($"Недостаточно наличности в банкомате. Запрошено {amount:C}, доступно {_remaining:C}", LogLevel.Warning);
            return false;
        }
        _remaining -= amount;
        DispenseCash(amount);
        return true;
    }

    private void DispenseCash(decimal amount) => Logger.Log($"Выдано {amount:C}"); 
}