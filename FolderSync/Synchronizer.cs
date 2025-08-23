using System.Security.Cryptography;

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
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CreateEmptyDirectories(ct);
            CopyOrUpdateSourceFilesToReplica(ct, sourceFiles, seen);
            DeleteReplicaFilesNotInSource(ct, seen);
            DeleteEmptyExtraDirectories(ct);

            return Task.CompletedTask;
        }

        private void CreateEmptyDirectories(CancellationToken ct)
        {
            var sourceDirs = Directory.EnumerateDirectories(_sourceRoot, "*", SearchOption.AllDirectories);
            foreach (var srcDir in sourceDirs)
            {
                ct.ThrowIfCancellationRequested();
                var rel = Path.GetRelativePath(_sourceRoot, srcDir);
                var dstDir = Path.Combine(_replicaRoot, rel);
                if (!Directory.Exists(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                    _logger.Action($"MKDIR  {rel}");
                }
            }
        }

        private void CopyOrUpdateSourceFilesToReplica(CancellationToken ct, List<string> sourceFiles,
            HashSet<string> seen)
        {
            foreach (var srcFile in sourceFiles)
            {
                ct.ThrowIfCancellationRequested();
                var rel = Path.GetRelativePath(_sourceRoot, srcFile);
                var dstFile = Path.Combine(_replicaRoot, rel);
                seen.Add(rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
                if (!File.Exists(dstFile))
                {
                    File.Copy(srcFile, dstFile, overwrite: false);
                    _logger.Action($"CREATE  {rel}");
                }
                else
                {
                    if (FilesDiffer(srcFile, dstFile))
                    {
                        File.Copy(srcFile, dstFile, overwrite: true);
                        _logger.Action($"UPDATE  {rel}");
                    }
                    else
                    {
                        _logger.Debug($"SKIP  {rel} (unchanged)");
                    }
                }
            }
        }

        private void DeleteReplicaFilesNotInSource(CancellationToken ct, HashSet<string> seen)
        {
            var replicaFiles = Directory.EnumerateFiles(_replicaRoot, "*", SearchOption.AllDirectories).ToList();
            foreach (var repFile in replicaFiles)
            {
                ct.ThrowIfCancellationRequested();
                var rel = Path.GetRelativePath(_replicaRoot, repFile);
                if (seen.Contains(rel)) continue;

                try
                {
                    File.Delete(repFile);
                    _logger.Action($"DELETE  {rel}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to delete extra file '{rel}': {ex.Message}");
                    _logger.Debug(ex.ToString());
                }
            }
        }

        private void DeleteEmptyExtraDirectories(CancellationToken ct)
        {
            var dirs = Directory.EnumerateDirectories(_replicaRoot, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length).ToList();
            foreach (var repDir in dirs)
            {
                ct.ThrowIfCancellationRequested();
                var rel = Path.GetRelativePath(_replicaRoot, repDir);
                var srcDir = Path.Combine(_sourceRoot, rel);
                if (!Directory.Exists(srcDir))
                {
                    try
                    {
                        Directory.Delete(repDir, recursive: true);
                        _logger.Action($"DELETE  {rel}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to remove extra directory '{rel}': {ex.Message}");
                        _logger.Debug(ex.ToString());
                    }
                }
            }
        }

        private static bool FilesDiffer(string fileA, string fileB)
        {
            var a = new FileInfo(fileA);
            var b = new FileInfo(fileB);
            if (a.Length != b.Length) return true;
            var dtA = a.LastWriteTimeUtc;
            var dtB = b.LastWriteTimeUtc;
            if (Math.Abs((dtA - dtB).TotalSeconds) < 2) return false;
            using var md5 = MD5.Create();
            using var sa = File.OpenRead(fileA);
            using var sb = File.OpenRead(fileB);
            var ha = md5.ComputeHash(sa);
            var hb = md5.ComputeHash(sb);
            return !ha.AsSpan().SequenceEqual(hb);
        }
    }
}