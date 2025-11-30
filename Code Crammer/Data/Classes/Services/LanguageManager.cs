#nullable enable
using Code_Crammer.Data.Classes.Core;
using Code_Crammer.Data.Classes.Models;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Code_Crammer.Data.Classes.Services
{
    public static class LanguageManager
    {
        private static readonly Dictionary<string, LanguageProfile> _extensionMap = new Dictionary<string, LanguageProfile>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Regex> _commentRegexCache = new Dictionary<string, Regex>();
        private static readonly Dictionary<string, Dictionary<string, Regex>> _distillRegexCache = new Dictionary<string, Dictionary<string, Regex>>();

        public static Action<string>? OnError;

        public static void Initialize()
        {
            _extensionMap.Clear();
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

                    if (profile != null && !string.IsNullOrEmpty(profile.Name) && profile.Extensions != null)
                    {
                        foreach (var ext in profile.Extensions)
                        {
                            if (!_extensionMap.ContainsKey(ext))
                            {
                                _extensionMap[ext] = profile;
                            }
                        }
                        PrecompileRegexes(profile);
                    }
                }
                catch (JsonException jex)
                {
                    OnError?.Invoke($"JSON Error in {Path.GetFileName(file)}: {jex.Message}");
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Failed to load language profile {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }

        public static LanguageProfile? GetProfileForExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return null;
            return _extensionMap.TryGetValue(extension, out var profile) ? profile : null;
        }

        public static Regex? GetCommentRegex(string languageName)
        {
            return _commentRegexCache.TryGetValue(languageName, out var regex) ? regex : null;
        }

        public static Dictionary<string, Regex>? GetDistillRegexes(string languageName)
        {
            return _distillRegexCache.TryGetValue(languageName, out var regexes) ? regexes : null;
        }

        private static void PrecompileRegexes(LanguageProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Name)) return;

            TimeSpan timeout = FileProcessor.GlobalRegexTimeout;
            bool ignoreCase = string.Equals(profile.Name, "Visual Basic", StringComparison.OrdinalIgnoreCase);
            var options = RegexOptions.Compiled;
            if (ignoreCase) options |= RegexOptions.IgnoreCase;

            if (!string.IsNullOrEmpty(profile.CommentPattern))
            {
                try
                {
                    _commentRegexCache[profile.Name] = new Regex(profile.CommentPattern, options, timeout);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Regex Error (Comments) in {profile.Name}: {ex.Message}");
                }
            }

            if (profile.DistillPatterns != null)
            {
                var patterns = new Dictionary<string, Regex>();
                foreach (var kvp in profile.DistillPatterns)
                {
                    try
                    {
                        patterns[kvp.Key] = new Regex(kvp.Value, options | RegexOptions.Multiline, timeout);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"Regex Error (Distill '{kvp.Key}') in {profile.Name}: {ex.Message}");
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