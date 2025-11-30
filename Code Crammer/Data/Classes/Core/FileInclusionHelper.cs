#nullable enable

using Code_Crammer.Data.Classes.Models;
using Code_Crammer.Data.Classes.Services;

namespace Code_Crammer.Data.Classes.Core
{
    public static class FileInclusionHelper
    {
        private static readonly HashSet<string> _excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".mp3", ".wav", ".mp4", ".zip", ".exe", ".dll", ".pdb", ".suo", ".user"
        };

        public static bool ShouldIncludeFile(string filePath, ScraperOptions options, out bool isCheckedByDefault)
        {
            isCheckedByDefault = true;
            if (string.IsNullOrEmpty(filePath)) return false;

            var fileInfo = new FileInfo(filePath);
            string fileExt = fileInfo.Extension;

            if (_excludedExtensions.Contains(fileExt)) return false;

            var langProfile = LanguageManager.GetProfileForExtension(fileExt);
            if (langProfile != null)
            {
                if (string.Equals(fileExt, ".cs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileExt, ".vb", StringComparison.OrdinalIgnoreCase))
                {
                    bool isDesigner = fileInfo.Name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                                      fileInfo.Name.EndsWith(".designer.vb", StringComparison.OrdinalIgnoreCase);

                    return isDesigner ? options.IncludeDesigner : options.IncludeCode;
                }
                return options.IncludeCode;
            }

            if (string.Equals(fileExt, ".csproj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileExt, ".vbproj", StringComparison.OrdinalIgnoreCase))
            {
                return options.IncludeProjectFile;
            }

            if (string.Equals(fileExt, ".resx", StringComparison.OrdinalIgnoreCase))
            {
                return options.IncludeResx;
            }

            if (string.Equals(fileExt, ".config", StringComparison.OrdinalIgnoreCase))
            {
                return options.IncludeConfig;
            }

            if (string.Equals(fileExt, ".json", StringComparison.OrdinalIgnoreCase))
            {
                return options.IncludeJson;
            }

            if (options.IncludeOtherFiles)
            {
                isCheckedByDefault = false;
                return true;
            }

            return false;
        }
    }
}