using ATM.Models;

namespace ATM.UI;

public class Keypad {
    public string GetInput() => Console.ReadLine() ?? string.Empty;

    public string? GetInput(TimeSpan timeout)
    {
        var readTask = Task.Run(() => Console.ReadLine());
        bool completed = readTask.Wait(timeout);
        if (!completed) return null;
        return readTask.Result ?? string.Empty;
    }

    public Pin GetPinInput() {
        Console.Write("PIN: ");
        return new Pin(GetInput());
    }

    public Pin? GetPinInput(TimeSpan timeout)
    {
        Console.Write("PIN: ");
        var input = GetInput(timeout);
        return input is null ? (Pin?)null : new Pin(input);
    }
}