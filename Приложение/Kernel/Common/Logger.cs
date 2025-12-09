using System.Runtime.CompilerServices;
using System.IO;

namespace ATM.Kernel.Common;

public enum LogLevel { Info, Warning, Error }

public static class Logger
{
    public static void Log(
        string message,
        LogLevel level = LogLevel.Info,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFilePath = "")
    {
        var className = string.IsNullOrWhiteSpace(callerFilePath)
            ? "Unknown"
            : Path.GetFileNameWithoutExtension(callerFilePath);

        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpper()}] [{className}.{memberName}] {message}");
    }
}