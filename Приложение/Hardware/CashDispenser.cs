using ATM.Common;

namespace ATM.Hardware;

public class CashDispenser { 
    public void DispenseCash(decimal amount) => Logger.Log($"Выдано {amount:C}"); 
}