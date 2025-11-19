#nullable disable

using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Code_Crammer.Data
{
    public static class LanguageManager
    {
        private static List<LanguageProfile> _profiles = new List<LanguageProfile>();
        private static Dictionary<string, Regex> _commentRegexCache = new Dictionary<string, Regex>();
        private static Dictionary<string, Dictionary<string, Regex>> _distillRegexCache = new Dictionary<string, Dictionary<string, Regex>>();

        public static void Initialize()
        {
            _profiles.Clear();
            _commentRegexCache.Clear();
            _distillRegexCache.Clear();

            string langFolder = PathManager.GetAddonsFolderPath();
            if (!Directory.Exists(langFolder))
            {
                Directory.CreateDirectory(langFolder);
            }

            string templatePath = Path.Combine(langFolder, "_TEMPLATE_EXAMPLE.json");
            if (!File.Exists(templatePath) && Directory.GetFiles(langFolder).Length == 0)
            {
                CreateTemplateFile(templatePath);
            }

            foreach (string file in Directory.GetFiles(langFolder, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var profile = JsonConvert.DeserializeObject<LanguageProfile>(json);

                    if (profile != null && profile.Extensions != null && profile.Extensions.Count > 0)
                    {
                        _profiles.Add(profile);
                        PrecompileRegexes(profile);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load language profile {file}: {ex.Message}");
                }
            }
        }

        public static LanguageProfile GetProfileForExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return null;
            return _profiles.FirstOrDefault(p => p.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }

        public static Regex GetCommentRegex(string languageName)
        {
            return _commentRegexCache.ContainsKey(languageName) ? _commentRegexCache[languageName] : null;
        }

        public static Dictionary<string, Regex> GetDistillRegexes(string languageName)
        {
            return _distillRegexCache.ContainsKey(languageName) ? _distillRegexCache[languageName] : null;
        }

        private static void PrecompileRegexes(LanguageProfile profile)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(1);

            if (!string.IsNullOrEmpty(profile.CommentPattern))
            {
                try
                {
                    _commentRegexCache[profile.Name] = new Regex(profile.CommentPattern, RegexOptions.Compiled, timeout);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error compiling comment regex for {profile.Name}: {ex.Message}");
                }
            }

            if (profile.DistillPatterns != null)
            {
                var patterns = new Dictionary<string, Regex>();
                foreach (var kvp in profile.DistillPatterns)
                {
                    try
                    {
                        patterns[kvp.Key] = new Regex(kvp.Value, RegexOptions.Compiled | RegexOptions.Multiline, timeout);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error compiling distill regex '{kvp.Key}' for {profile.Name}: {ex.Message}");
                    }
                }
                _distillRegexCache[profile.Name] = patterns;
            }
        }

        private static void CreateTemplateFile(string path)
        {
            var template = new LanguageProfile
            {
                Name = "ExampleLanguage",
                Extensions = new List<string> { ".ex", ".example" },
                CommentPattern = "(//.*)|(/\\*[\\s\\S]*?\\*/)",
                DistillPatterns = new Dictionary<string, string>
                {
                    { "FUNCTION", "^\\s*function\\s+(\\w+)" },
                    { "CLASS", "^\\s*class\\s+(\\w+)" }
                }
            };

            File.WriteAllText(path, JsonConvert.SerializeObject(template, Formatting.Indented));
        }
    }
}