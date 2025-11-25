using ATM.Kernel.Common;

namespace ATM.Kernel.Hardware;

public class ReceiptPrinter { 
    public void PrintReceipt(string text) => Logger.Log($"Печать чека: {text}"); 
}

