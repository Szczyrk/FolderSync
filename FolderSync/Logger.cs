namespace FolderSync
{
    internal sealed class Logger : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly object _lock = new();

        public Logger(string logFilePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logFilePath))!);
            _writer = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };
        }

        public void Info(string message) => Write("INFO", message, ConsoleColor.Cyan);
        public void Action(string message) => Write("SYNC", message, ConsoleColor.Green);
        public void Error(string message) => Write("ERROR", message, ConsoleColor.Red);
        public void Debug(string message) => Write("DEBUG", message, ConsoleColor.DarkGray);

        private void Write(string level, string message, ConsoleColor color)
        {
            var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"[{timestamp}] [{level}] {message}";
            lock (_lock)
            {
                var prev = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(line);
                }
                finally
                {
                    Console.ForegroundColor = prev;
                }

                _writer.WriteLine(line);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer.Dispose();
            }
        }
    }
}