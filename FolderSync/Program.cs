using System;

namespace FolderSync
{
    internal static class Program
    {
        public static int Main(string[] args)
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

            Console.WriteLine("OK.");
            return 0;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"
FolderSync — one-way folder synchronization

Usage:
  dotnet run -- --source <SOURCE_PATH> --replica <REPLICA_PATH> --interval <SECONDS> --log <LOG_FILE>");
        }
    }
}