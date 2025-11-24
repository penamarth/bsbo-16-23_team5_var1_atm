using ATM.Kernel.Common;

namespace ATM.Contexts.Hardware;

public class ReceiptPrinter { 
    public void PrintReceipt(string text) => Logger.Log($"Печать чека: {text}"); 
}