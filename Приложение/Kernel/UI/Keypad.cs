using ATM.Kernel.Models;

namespace ATM.Kernel.UI;

public class Keypad {
    public string? GetInput() {
        return Console.ReadLine();
    }

    public string? GetInput(TimeSpan timeout) {
        var cts = new CancellationTokenSource(timeout);
        try {
            var task = Task.Run(() => Console.ReadLine(), cts.Token);
            task.Wait(cts.Token);
            return task.Result;
        } catch (OperationCanceledException) {
            return null;
        }
    }

    public Pin? GetPinInput(TimeSpan timeout) {
        var input = GetInput(timeout);
        if (string.IsNullOrWhiteSpace(input)) {
            return null;
        }
        return new Pin(input);
    }
}

