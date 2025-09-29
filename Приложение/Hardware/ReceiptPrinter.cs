using ATM.Common;

namespace ATM.Hardware;

public class ReceiptPrinter { 
    public void PrintReceipt(string text) => Logger.Log($"Печать чека: {text}"); 
}