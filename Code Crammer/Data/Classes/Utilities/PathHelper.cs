#nullable enable

using System.Diagnostics;

namespace Code_Crammer.Data.Classes.Utilities
{
    public static class PathHelper
    {
        private static readonly HashSet<string> _excludeFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".vs", ".git", "node_modules", "packages", ".idea", ".vscode"
        };

        public static string GetSafeRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrWhiteSpace(relativeTo) || string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {

                if (path.Contains(".."))
                {
                    Debug.WriteLine($"Security Block: Path '{path}' contains traversal characters.");
                    return string.Empty;
                }

                string fullBase = Path.GetFullPath(relativeTo);
                string fullTarget = Path.GetFullPath(path);

                if (!fullTarget.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                string relative = Path.GetRelativePath(fullBase, fullTarget);

                if (relative.StartsWith("..") || Path.IsPathRooted(relative))
                {
                    Debug.WriteLine($"Security Block: Path '{path}' resolves to '{relative}' which is outside root.");
                    return string.Empty;
                }

                return relative;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Path Error: {ex.Message}");
                return string.Empty;
            }
        }

        public static List<string> SafeGetFiles(string path)
        {
            var files = new List<string>();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return files;

            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string fullStartPath = Path.GetFullPath(path);
            queue.Enqueue(fullStartPath);
            visited.Add(fullStartPath);

            while (queue.Count > 0)
            {
                var currentDir = queue.Dequeue();

                try
                {
                    var dirFiles = Directory.GetFiles(currentDir);
                    foreach (var file in dirFiles)
                    {
                        files.Add(file);
                    }

                    foreach (var dir in Directory.GetDirectories(currentDir))
                    {
                        string dirName = Path.GetFileName(dir);
                        if (_excludeFolders.Contains(dirName)) continue;

                        if (visited.Add(dir))
                        {
                            queue.Enqueue(dir);
                        }
                    }
                }
                catch (UnauthorizedAccessException uex)
                {
                    Debug.WriteLine($"Access Denied skipping folder {currentDir}: {uex.Message}");
                }
                catch (PathTooLongException pex)
                {
                    Debug.WriteLine($"Path too long skipping folder {currentDir}: {pex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error scanning {currentDir}: {ex.Message}");
                }
            }

            return files;
        }

        public static bool IsFolderExcluded(string folderName)
        {
            return _excludeFolders.Contains(folderName);
        }

        public static bool IsPathIgnored(string filePath)
        {
            foreach (string excludedDir in _excludeFolders)
            {
                if (filePath.IndexOf($"{Path.DirectorySeparatorChar}{excludedDir}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}