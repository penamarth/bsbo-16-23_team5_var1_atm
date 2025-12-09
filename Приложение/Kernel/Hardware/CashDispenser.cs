using ATM.Kernel.Common;

namespace ATM.Kernel.Hardware;

public class CashDispenser { 
    private decimal _remaining;

    public CashDispenser(decimal initialCash = 0m)
    {
        _remaining = initialCash;
    }

    public decimal Remaining => _remaining;

    public bool CanDispense(decimal amount)
    {
        var can = amount > 0 && amount <= _remaining;
        Logger.Log(
            $"Проверка возможности выдать {amount:C}. Доступно: {_remaining:C}. Результат: {(can ? "достаточно" : "недостаточно")}",
            can ? LogLevel.Info : LogLevel.Warning);
        return can;
    }

    public void Refill(decimal amount) {
        if (amount <= 0) return;
        _remaining += amount;
        Logger.Log($"Инкассация: загружено {amount:C}. Текущий остаток: {_remaining:C}");
    }

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

