using System;

namespace FolderSync
{
    internal static class Program
    {
        private static async Task<int> RunAsync(string[] args)
        {
            string? source = null, replica = null, log = null;
            int interval = 0;

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.Equals("--source", StringComparison.OrdinalIgnoreCase)) source = args[++i];
                else if (a.Equals("--replica", StringComparison.OrdinalIgnoreCase)) replica = args[++i];
                else if (a.Equals("--log", StringComparison.OrdinalIgnoreCase)) log = args[++i];
                else if (a.Equals("--interval", StringComparison.OrdinalIgnoreCase)) interval = int.Parse(args[++i]);
                else if (a is "-h" or "--help" or "/?")
                {
                    PrintUsage();
                    return 0;
                }
            }

            if (string.IsNullOrWhiteSpace(source) ||
                string.IsNullOrWhiteSpace(replica) ||
                string.IsNullOrWhiteSpace(log) ||
                interval <= 0)
            {
                PrintUsage();
                return 2;
            }

            source = Path.GetFullPath(source);
            replica = Path.GetFullPath(replica);
            log = Path.GetFullPath(log);

            if (!Directory.Exists(source))
            {
                await Console.Error.WriteLineAsync($"Source folder does not exist: {source}");
                return 3;
            }

            Directory.CreateDirectory(replica);

            using var logger = new Logger(log);
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var sync = new Synchronizer(source, replica, logger);

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    logger.Info($"--- Synchronization cycle started ---");
                    await sync.SyncOnceAsync(cts.Token);
                    logger.Info($"--- Synchronization cycle finished ---");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.Error($"Unexpected error: {ex.Message}");
                    logger.Debug(ex.ToString());
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(interval), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            logger.Info("Exiting.");
            return 0;
        }

        private static int Main(string[] args) => RunAsync(args).GetAwaiter().GetResult();

        private static void PrintUsage()
        {
            Console.WriteLine(@"
FolderSync — one-way folder synchronization

Usage:
  dotnet run -- --source <SOURCE_PATH> --replica <REPLICA_PATH> --interval <SECONDS> --log <LOG_FILE>");
        }
    }
}