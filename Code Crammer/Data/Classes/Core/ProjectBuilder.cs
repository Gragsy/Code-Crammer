#nullable enable
using Code_Crammer.Data.Classes.Models;
using Code_Crammer.Data.Classes.Utilities;
using System.Text;

namespace Code_Crammer.Data.Classes.Core
{
    public static class ProjectBuilder
    {

        private const int MAX_CACHE_SIZE = 200;
        private readonly record struct FileCacheKey(string FilePath, long LastWriteTick, int OptionsFlag);
        private static readonly LRUCache<FileCacheKey, string> _processedContentCache = new LRUCache<FileCacheKey, string>(MAX_CACHE_SIZE);

        public static void ClearCache()
        {
            _processedContentCache.Clear();
        }

        public static long GetApproximateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            double estimatedTokens = text.Length / AppConstants.TokenCharsPerToken;
            return (long)Math.Ceiling(estimatedTokens * AppConstants.TokenOverheadMultiplier);
        }

        public static long GetTokenCountForFile(string filePath, string solutionPath, ScraperOptions options)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > 1048576)
                {
                    return (long)Math.Ceiling((fileInfo.Length / AppConstants.TokenCharsPerToken) * AppConstants.TokenOverheadMultiplier);
                }

                string processedContent = GetCachedProcessedFileContentSync(filePath, solutionPath, options, false);
                return GetApproximateTokenCount(processedContent);
            }
            catch
            {
                return 0;
            }
        }

        public static async Task<string> GenerateProjectStateStringAsync(
            string solutionPath,
            ScraperOptions options,
            List<string> allFiles,
            HashSet<string> checkedNodeTags,
            HashSet<string> selectedFiles,
            List<string> cachedProjectFiles,
            IProgress<string>? progress,
            CancellationToken ct,
            string projectStructure,
            string messageContent)
        {
            try
            {
                StringBuilder report = new StringBuilder();
                report.AppendLine($"CODE CRAMMER - {Path.GetFileName(solutionPath)}");
                report.AppendLine($"Generated on: {DateTime.Now}");
                report.AppendLine();

                if (options.IncludeFolderLayout && !string.IsNullOrEmpty(projectStructure))
                {
                    report.AppendLine(AppConstants.HeaderProjectStructure);
                    report.AppendLine(AppConstants.MarkdownBlockStart);
                    report.AppendLine(projectStructure);
                    report.AppendLine(AppConstants.MarkdownBlockEnd);
                    report.AppendLine();
                }

                progress?.Report("Grouping files by project...");
                var projectGroups = await Task.Run(() => GroupFilesByProject(solutionPath, allFiles, checkedNodeTags, selectedFiles, options, cachedProjectFiles), ct);

                ct.ThrowIfCancellationRequested();

                string fileReportPart = await BuildReportFromGroupedFilesAsync(solutionPath, projectGroups, selectedFiles, options, cachedProjectFiles, progress, ct);
                report.Append(fileReportPart);

                ct.ThrowIfCancellationRequested();

                if (options.IncludeMessage && !string.IsNullOrWhiteSpace(messageContent))
                {
                    report.AppendLine(messageContent);
                }

                return report.ToString();
            }
            catch (OperationCanceledException)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                progress?.Report($"FATAL ERROR: {ex.Message}");
                return string.Empty;
            }
        }

        private static Dictionary<string, List<string>> GroupFilesByProject(
            string solutionPath,
            List<string> allFiles,
            HashSet<string> checkedNodeTags,
            HashSet<string> selectedFiles,
            ScraperOptions options,
            List<string> cachedProjectFiles)
        {
            var projectGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            projectGroups.Add(string.Empty, new List<string>());

            if (cachedProjectFiles.Count == 0)
            {
                string rootName = new DirectoryInfo(solutionPath).Name;
                displayNames[solutionPath] = rootName;
                if (!projectGroups.ContainsKey(solutionPath))
                {
                    projectGroups.Add(solutionPath, new List<string>());
                }
            }

            foreach (var projPath in cachedProjectFiles)
            {
                string projDir = Path.GetDirectoryName(projPath) ?? string.Empty;
                if (!string.IsNullOrEmpty(projDir) && !projectGroups.ContainsKey(projDir))
                {
                    projectGroups.Add(projDir, new List<string>());
                    displayNames.Add(projDir, Path.GetFileNameWithoutExtension(projPath));
                }
            }

            var sortedProjectKeys = projectGroups.Keys
                .Where(k => !string.IsNullOrEmpty(k))
                .OrderByDescending(k => k.Length)
                .ToList();

            bool isAnyDistillModeActive = options.DistillUnused || options.DistillUnusedHeaders;
            List<string> pathsToProcess;

            if (isAnyDistillModeActive)
            {
                if (options.DistillUnusedHeaders)
                {
                    // Logic for DistillUnusedHeaders (Active Projects Only)
                    // We check if the PROJECT DIRECTORY is in the checked tags

                    var activeProjectDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // Check solution root
                    if (checkedNodeTags.Contains(solutionPath))
                    {
                        activeProjectDirs.Add(solutionPath);
                    }

                    foreach (var projDir in displayNames.Keys)
                    {
                        string relPath = PathHelper.GetSafeRelativePath(solutionPath, projDir);
                        if (checkedNodeTags.Contains(relPath) || checkedNodeTags.Contains(projDir))
                        {
                            activeProjectDirs.Add(projDir);
                        }
                    }

                    bool isSolutionItemsChecked = checkedNodeTags.Contains(solutionPath); // Root check acts as solution items check usually

                    pathsToProcess = allFiles.Where(relPath =>
                    {
                        string fullPath = Path.Combine(solutionPath, relPath);
                        string? parentProjDir = sortedProjectKeys.FirstOrDefault(dir =>
                        {
                            string dirWithSep = dir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? dir : dir + Path.DirectorySeparatorChar;
                            return fullPath.StartsWith(dirWithSep, StringComparison.OrdinalIgnoreCase);
                        });

                        if (parentProjDir != null)
                        {
                            return activeProjectDirs.Contains(parentProjDir);
                        }

                        return isSolutionItemsChecked && (Path.GetDirectoryName(fullPath) ?? "").Equals(solutionPath, StringComparison.OrdinalIgnoreCase);
                    }).ToList();
                }
                else
                {
                    pathsToProcess = allFiles;
                }
            }
            else
            {
                pathsToProcess = selectedFiles.ToList();
            }

            foreach (var relativePath in pathsToProcess)
            {
                string fullPath = Path.Combine(solutionPath, relativePath);
                string? parentProjectDir = sortedProjectKeys.FirstOrDefault(dir => fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase));

                if (parentProjectDir != null)
                {
                    projectGroups[parentProjectDir].Add(fullPath);
                }
                else
                {
                    projectGroups[string.Empty].Add(fullPath);
                }
            }

            return projectGroups;
        }

        private static async Task<string> BuildReportFromGroupedFilesAsync(
            string solutionPath,
            Dictionary<string, List<string>> projectGroups,
            HashSet<string> selectedFiles,
            ScraperOptions options,
            List<string> cachedProjectFiles,
            IProgress<string>? progress,
            CancellationToken ct)
        {
            StringBuilder report = new StringBuilder();
            var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (cachedProjectFiles.Count == 0)
            {
                string rootName = new DirectoryInfo(solutionPath).Name;
                displayNames.TryAdd(solutionPath, rootName);
            }
            else
            {
                foreach (var projPath in cachedProjectFiles)
                {
                    string? dir = Path.GetDirectoryName(projPath);
                    if (dir != null)
                    {
                        displayNames.TryAdd(dir, Path.GetFileNameWithoutExtension(projPath));
                    }
                }
            }

            foreach (var groupKey in projectGroups.Keys.OrderBy(k => string.IsNullOrEmpty(k) ? " " : displayNames.GetValueOrDefault(k, k)))
            {
                ct.ThrowIfCancellationRequested();
                string header = string.IsNullOrEmpty(groupKey) ? AppConstants.HeaderSolutionItems : $"{AppConstants.HeaderProjectPrefix}{displayNames.GetValueOrDefault(groupKey, "Unknown")}";

                var filesInGroup = projectGroups[groupKey].Distinct().OrderBy(f => f).ToList();
                if (!filesInGroup.Any()) continue;

                report.AppendLine(header);
                report.AppendLine();

                foreach (var filePath in filesInGroup)
                {
                    ct.ThrowIfCancellationRequested();
                    string currentRelativePath = PathHelper.GetSafeRelativePath(solutionPath, filePath);
                    progress?.Report($"...adding {currentRelativePath}");

                    try
                    {
                        bool isSelected = selectedFiles.Contains(currentRelativePath);
                        bool forceDistill = (options.DistillUnused || options.DistillUnusedHeaders) && !isSelected;

                        string fileContent = await GetCachedProcessedFileContentAsync(filePath, solutionPath, options, forceDistill);

                        if (string.IsNullOrWhiteSpace(fileContent))
                        {
                            continue;
                        }

                        report.AppendLine($"{AppConstants.HeaderFilePrefix}{currentRelativePath} ---");
                        string ext = Path.GetExtension(filePath).ToLowerInvariant();
                        string mdTag = GetMarkdownTag(ext);
                        report.AppendLine($"{AppConstants.MarkdownBlockStart}{mdTag}");
                        report.AppendLine(fileContent);
                        report.AppendLine(AppConstants.MarkdownBlockEnd);
                        report.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"    ...error reading file {currentRelativePath}: {ex.Message}");
                    }
                }
            }

            return report.ToString();
        }

        private static async Task<string> GetCachedProcessedFileContentAsync(string filePath, string rootPath, ScraperOptions options, bool forceDistill)
        {
            if (string.IsNullOrEmpty(PathHelper.GetSafeRelativePath(rootPath, filePath))) return string.Empty;
            if (!File.Exists(filePath)) return string.Empty;

            long lastWriteTick = new FileInfo(filePath).LastWriteTimeUtc.Ticks;
            int boolState = GetOptionsFlag(options, forceDistill);

            var cacheKey = new FileCacheKey(filePath, lastWriteTick, boolState);

            if (_processedContentCache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            string fileContent = string.Empty;
            try
            {

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    fileContent = await sr.ReadToEndAsync();
                }
            }
            catch
            {
                return string.Empty;
            }

            string processedContent = FileProcessor.ProcessFile(filePath, fileContent, options, forceDistill);
            _processedContentCache.Add(cacheKey, processedContent);
            return processedContent;
        }

        private static string GetCachedProcessedFileContentSync(string filePath, string rootPath, ScraperOptions options, bool forceDistill)
        {
            if (string.IsNullOrEmpty(PathHelper.GetSafeRelativePath(rootPath, filePath))) return string.Empty;
            if (!File.Exists(filePath)) return string.Empty;

            long lastWriteTick = new FileInfo(filePath).LastWriteTimeUtc.Ticks;
            int boolState = GetOptionsFlag(options, forceDistill);

            var cacheKey = new FileCacheKey(filePath, lastWriteTick, boolState);

            if (_processedContentCache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            string fileContent = string.Empty;
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    fileContent = sr.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }

            string processedContent = FileProcessor.ProcessFile(filePath, fileContent, options, forceDistill);
            _processedContentCache.Add(cacheKey, processedContent);
            return processedContent;
        }

        private static int GetOptionsFlag(ScraperOptions options, bool forceDistill)
        {
            int boolState = 0;
            if (options.SanitizeOutput) boolState |= 1 << 0;
            if (options.RemoveComments) boolState |= 1 << 1;
            if (options.SquishDesignerFiles) boolState |= 1 << 2;
            if (options.DistillProject) boolState |= 1 << 3;
            if (options.DistillUnusedHeaders) boolState |= 1 << 4;
            if (options.DistillUnused) boolState |= 1 << 5;
            if (forceDistill) boolState |= 1 << 6;
            return boolState;
        }

        private static readonly Dictionary<string, string> _markdownTagMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".cs", "csharp" }, { ".csx", "csharp" },
            { ".vb", "vb" },
            { ".json", "json" },
            { ".xml", "xml" }, { ".csproj", "xml" }, { ".vbproj", "xml" }, { ".config", "xml" }, { ".xaml", "xml" }, { ".svg", "xml" },
            { ".sql", "sql" },
            { ".js", "javascript" }, { ".jsx", "javascript" },
            { ".ts", "typescript" }, { ".tsx", "typescript" },
            { ".css", "css" }, { ".scss", "css" }, { ".less", "css" },
            { ".html", "html" }, { ".htm", "html" }, { ".razor", "html" },
            { ".py", "python" }, { ".pyw", "python" },
            { ".md", "markdown" },
            { ".sh", "bash" }, { ".bash", "bash" },
            { ".ps1", "powershell" }, { ".psm1", "powershell" },
            { ".cpp", "cpp" }, { ".h", "cpp" }, { ".hpp", "cpp" }, { ".c", "cpp" }, { ".cc", "cpp" },
            { ".java", "java" }, { ".jar", "java" },
            { ".go", "go" },
            { ".rs", "rust" },
            { ".php", "php" },
            { ".rb", "ruby" },
            { ".lua", "lua" },
            { ".yaml", "yaml" }, { ".yml", "yaml" },
            { ".ini", "ini" },
            { ".dockerfile", "dockerfile" },
            { ".swift", "swift" },
            { ".kt", "kotlin" }, { ".kts", "kotlin" }
        };

        private static string GetMarkdownTag(string ext)
        {
            return _markdownTagMap.TryGetValue(ext, out string? tag) ? tag : "";
        }
    }
}