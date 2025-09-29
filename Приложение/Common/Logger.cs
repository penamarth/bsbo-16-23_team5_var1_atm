namespace ATM.Common {
    public enum LogLevel { Info, Warning, Error }

    public static class Logger {
        public static void Log(string message, LogLevel level = LogLevel.Info) {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpper()}] {message}");
        }
    }
}