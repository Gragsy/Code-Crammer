using Code_Crammer.Data.Classes.Components;
using Code_Crammer.Data.Classes.Models;

namespace Code_Crammer.Data.Classes.Core
{
    public class TokenCounter : IDisposable
    {
        private CancellationTokenSource? _cts;
        private int _sequence = 0;
        private bool _disposed;

        public async Task<long> CountTokensAsync(
            string solutionPath,
            ScraperOptions options,
            TreeViewManager treeManager,
            List<string> cachedProjectFiles,
            string messageContent,
            IProgress<string> progress)
        {
            Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            int mySequence = Interlocked.Increment(ref _sequence);

            try
            {
                var allFiles = treeManager.GetAllFilePaths();
                var checkedProjects = treeManager.GetCheckedTopLevelNodes();
                var selectedFiles = treeManager.GetCheckedFiles();
                string structureString = options.IncludeFolderLayout ? treeManager.GetTreeAsText() : "";

                string finalOutput = await ProjectBuilder.GenerateProjectStateStringAsync(
                    solutionPath,
                    options,
                    allFiles,
                    checkedProjects,
                    selectedFiles,
                    cachedProjectFiles,
                    progress,
                    token,
                    structureString,
                    messageContent);

                if (token.IsCancellationRequested) return 0;
                if (mySequence != _sequence) return 0;

                progress?.Report("Calculating (Finalizing...)");
                return await Task.Run(() => ProjectBuilder.GetApproximateTokenCount(finalOutput));
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public void Cancel()
        {
            if (_cts != null)
            {
                try
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
                catch { }
                finally
                {
                    _cts = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Cancel();
                }
                _disposed = true;
            }
        }
    }
}