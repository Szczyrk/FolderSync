namespace FolderSync
{
    internal sealed class Synchronizer
    {
        private readonly string _sourceRoot;
        private readonly string _replicaRoot;
        private readonly Logger _logger;

        public Synchronizer(string sourceRoot, string replicaRoot, Logger logger)
        {
            _sourceRoot = sourceRoot;
            _replicaRoot = replicaRoot;
            _logger = logger;
        }

        public Task SyncOnceAsync(CancellationToken ct)
        {
            var sourceFiles = Directory.EnumerateFiles(_sourceRoot, "*", SearchOption.AllDirectories).ToList();
            foreach (var srcFile in sourceFiles)
            {
                ct.ThrowIfCancellationRequested();
                var rel = Path.GetRelativePath(_sourceRoot, srcFile);
                var dstFile = Path.Combine(_replicaRoot, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
                if (!File.Exists(dstFile))
                {
                    File.Copy(srcFile, dstFile, overwrite: false);
                    _logger.Action($"CREATE  {rel}");
                }
            }

            return Task.CompletedTask;
        }
    }
}