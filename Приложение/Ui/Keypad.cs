using ATM.Models;

namespace ATM.UI;

public class Keypad {
    public string GetInput() => Console.ReadLine() ?? string.Empty;

    public Pin GetPinInput() {
        Console.Write("PIN: ");
        return new Pin(GetInput());
    }
}