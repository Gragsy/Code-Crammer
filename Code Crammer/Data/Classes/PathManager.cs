#nullable disable

namespace Code_Crammer.Data
{
    public static class PathManager
    {
        private const int MAX_DIRECTORY_DEPTH_SEARCH = 6;
        private static string _solutionRootPath;

        private static string FindSolutionRoot()
        {
            if (!string.IsNullOrEmpty(_solutionRootPath))
            {
                return _solutionRootPath;
            }

            DirectoryInfo currentDir = new DirectoryInfo(Application.StartupPath);
            for (int i = 0; i < MAX_DIRECTORY_DEPTH_SEARCH; i++)
            {
                if (currentDir == null) break;

                if (Directory.GetFiles(currentDir.FullName, "*.sln").Any() ||
                    Directory.GetFiles(currentDir.FullName, "*.csproj").Any())
                {
                    _solutionRootPath = currentDir.FullName;
                    return _solutionRootPath;
                }

                currentDir = currentDir.Parent;
                if (currentDir == null) break;
            }

            _solutionRootPath = Application.StartupPath;
            return _solutionRootPath;
        }

        public static string GetProfilesFolderPath()
        {
            return EnsureDirectoryExists(Path.Combine(FindSolutionRoot(), "Data", "Profiles"),
                                       Path.Combine(Application.StartupPath, "Data", "Profiles"));
        }

        public static string GetDataFolderPath()
        {
            return EnsureDirectoryExists(Path.Combine(FindSolutionRoot(), "Data"),
                                       Path.Combine(Application.StartupPath, "Data"));
        }

        public static string GetDictionariesFolderPath()
        {
            return EnsureDirectoryExists(Path.Combine(GetDataFolderPath(), "Dictionaries"), null);
        }

        public static string GetAddonsFolderPath()
        {
            return EnsureDirectoryExists(Path.Combine(GetDataFolderPath(), "Addons"),
                                       Path.Combine(Application.StartupPath, "Data", "Addons"));
        }

        private static string EnsureDirectoryExists(string preferredPath, string fallbackPath)
        {
            if (Directory.Exists(preferredPath)) return preferredPath;

            if (!string.IsNullOrEmpty(fallbackPath) && Directory.Exists(fallbackPath)) return fallbackPath;

            Directory.CreateDirectory(preferredPath);
            return preferredPath;
        }
    }
}