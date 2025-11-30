#nullable enable

using Code_Crammer.Data.Classes.Models;
using Code_Crammer.Data.Classes.Services;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Code_Crammer.Data.Classes.Core
{
    public static class FileProcessor
    {
        public static readonly TimeSpan GlobalRegexTimeout = TimeSpan.FromSeconds(2);

        private static readonly HashSet<string> _designerIgnoreList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "Resources.Designer.cs", "Settings.Designer.cs", "Reference.cs"
};

        private static readonly Regex _csFieldRegex = new Regex(
            @"(?:private|public|protected|internal)\s+(?:[\w]+\.)*(\w+)\s+(\w+);",
            RegexOptions.Compiled, GlobalRegexTimeout);

        private static readonly Dictionary<string, Regex> _csPropertyRegexes = new Dictionary<string, Regex>
{
    { "Loc", new Regex(@"(?:this\.)?(\w+)\.Location\s*=\s*new\s*(?:System\.Drawing\.)?Point\s*\(\s*(\d+\s*,\s*\d+)\s*\);", RegexOptions.None, GlobalRegexTimeout) },
    { "Size", new Regex(@"(?:this\.)?(\w+)\.Size\s*=\s*new\s*(?:System\.Drawing\.)?Size\s*\(\s*(\d+\s*,\s*\d+)\s*\);", RegexOptions.None, GlobalRegexTimeout) },
    { "Text", new Regex(@"(?:this\.)?(\w+)\.Text\s*=\s*""((?:\\""|[^""])*)"";", RegexOptions.Singleline, GlobalRegexTimeout) },
    { "BackColor", new Regex(@"(?:this\.)?(\w+)\.BackColor\s*=\s*(?:System\.Drawing\.)?Color\.(\w+);", RegexOptions.None, GlobalRegexTimeout) },
    { "Anchor", new Regex(@"(?:this\.)?(\w+)\.Anchor\s*=\s*\((.*?)\);", RegexOptions.None, GlobalRegexTimeout) }
};

        private static readonly Regex _vbFieldRegex = new Regex(
            @"(?:Public|Friend|Private|Protected)\s+WithEvents\s+(\w+)\s+As\s+(?:[a-zA-Z_]\w*\.)*(\w+)",
            RegexOptions.IgnoreCase, GlobalRegexTimeout);

        private static readonly Dictionary<string, Regex> _vbPropertyRegexes = new Dictionary<string, Regex>
{
    { "Loc", new Regex(@"(\w+)\.Location\s*=\s*New\s*(?:System\.Drawing\.)?Point\s*\(\s*(\d+\s*,\s*\d+)\s*\)", RegexOptions.IgnoreCase, GlobalRegexTimeout) },
    { "Size", new Regex(@"(\w+)\.Size\s*=\s*New\s*(?:System\.Drawing\.)?Size\s*\(\s*(\d+\s*,\s*\d+)\s*\)", RegexOptions.IgnoreCase, GlobalRegexTimeout) },
    { "Text", new Regex(@"(\w+)\.Text\s*=\s*""((?:""""|[^""])*)""", RegexOptions.Singleline | RegexOptions.IgnoreCase, GlobalRegexTimeout) },
    { "BackColor", new Regex(@"(\w+)\.BackColor\s*=\s*(?:System\.Drawing\.)?Color\.(\w+)", RegexOptions.IgnoreCase, GlobalRegexTimeout) },
    { "Anchor", new Regex(@"(\w+)\.Anchor\s*=\s*\((.*?)\)", RegexOptions.IgnoreCase, GlobalRegexTimeout) }
};

        public static string ProcessFile(string filePath, string content, ScraperOptions options, bool forceDistill)
        {
            string fileName = Path.GetFileName(filePath);
            string fileExt = Path.GetExtension(fileName).ToLowerInvariant();

            bool isDesigner = IsDesignerFile(fileName) && !IsIgnoredDesigner(fileName);

            if (forceDistill && options.DistillUnusedHeaders)
            {
                return GenerateOmittedHeader(fileExt);
            }

            if (forceDistill || (options.DistillProject && !isDesigner))
            {
                return ProcessDistilledFile(content, fileExt, isDesigner);
            }

            if (isDesigner && options.SquishDesignerFiles)
            {
                return ProcessDesignerSquish(content, fileExt);
            }

            var languageProfile = LanguageManager.GetProfileForExtension(fileExt);
            if (languageProfile != null)
            {
                content = SanitizeCodeContent(content, languageProfile.Name, options);
            }
            else if (options.SanitizeOutput)
            {
                if (fileExt == ".resx") return SanitizeResxContent(filePath, content);
                if (fileExt == ".csproj" || fileExt == ".vbproj") return SanitizeCsprojContent(filePath, content);
            }

            return content;
        }

        private static bool IsDesignerFile(string fileName)
        {
            return fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".designer.vb", StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateOmittedHeader(string fileExt)
        {
            var lang = LanguageManager.GetProfileForExtension(fileExt);
            string commentChar = (lang != null && lang.Name == "Visual Basic") ? "'" : "//";
            return $"{commentChar} [Content Omitted]";
        }

        private static string ProcessDistilledFile(string content, string fileExt, bool isDesigner)
        {
            if (isDesigner) return string.Empty;

            var profile = LanguageManager.GetProfileForExtension(fileExt);
            if (profile != null)
            {
                var patterns = LanguageManager.GetDistillRegexes(profile.Name);
                if (patterns != null)
                {
                    return DistillContent(content, patterns);
                }
            }
            return string.Empty;
        }

        private static string ProcessDesignerSquish(string content, string fileExt)
        {
            if (fileExt == ".cs") return SquishDesignerContent(content, true);
            if (fileExt == ".vb") return SquishDesignerContent(content, false);
            return content;
        }

        private static string DistillContent(string fileContent, Dictionary<string, Regex> patterns)
        {
            if (string.IsNullOrEmpty(fileContent)) return string.Empty;

            StringBuilder sb = new StringBuilder();
            var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                foreach (var kvp in patterns)
                {
                    Match match = kvp.Value.Match(line);
                    if (match.Success)
                    {
                        string capturedName = match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();
                        sb.AppendLine($"{kvp.Key}: {capturedName}");
                        break;
                    }
                }
            }
            return sb.ToString().Trim();
        }

        private static string SanitizeCodeContent(string content, string languageName, ScraperOptions options)
        {
            string processed = content;

            if (options.RemoveComments)
            {
                var regex = LanguageManager.GetCommentRegex(languageName);
                if (regex != null)
                {
                    try
                    {
                        processed = regex.Replace(processed, m =>
                        {

                            if (m.Groups.Count > 1 && m.Groups[1].Success)
                            {
                                return m.Value;
                            }

                            bool isVb = string.Equals(languageName, "Visual Basic", StringComparison.OrdinalIgnoreCase);
                            if (!isVb && m.Groups.Count > 2 && m.Groups[2].Success)
                            {
                                return m.Value;
                            }

                            return string.Empty;
                        });
                    }
                    catch (RegexMatchTimeoutException)
                    {

                        System.Diagnostics.Debug.WriteLine($"Regex timeout removing comments for {languageName}");
                        return content + Environment.NewLine + $"/* WARNING: Comment removal skipped due to complexity timeout in {languageName} parser. */";
                    }
                }
            }

            if (options.SanitizeOutput)
            {
                StringBuilder sb = new StringBuilder();
                using (var reader = new StringReader(processed))
                {
                    string? line;
                    bool preserveIndentation = string.Equals(languageName, "Python", StringComparison.OrdinalIgnoreCase);

                    while ((line = reader.ReadLine()) != null)
                    {
                        string trimmedLine = preserveIndentation ? line.TrimEnd() : line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            sb.AppendLine(trimmedLine);
                        }
                    }
                }
                return sb.ToString().Trim();
            }

            return processed;
        }

        private static string SquishDesignerContent(string content, bool isCs)
        {
            if (string.IsNullOrWhiteSpace(content)) return "/* Empty Designer File */";

            try
            {
                var controlData = new Dictionary<string, (string Type, List<string> Properties)>(StringComparer.OrdinalIgnoreCase);
                Regex fieldRegex = isCs ? _csFieldRegex : _vbFieldRegex;
                var patterns = isCs ? _csPropertyRegexes : _vbPropertyRegexes;

                foreach (Match m in fieldRegex.Matches(content))
                {
                    if (m.Success && m.Groups.Count >= 3)
                    {
                        if (isCs)
                            controlData[m.Groups[2].Value] = (m.Groups[1].Value, new List<string>());
                        else
                            controlData[m.Groups[1].Value] = (m.Groups[2].Value, new List<string>());
                    }
                }

                foreach (var kvp in patterns)
                {
                    foreach (Match m in kvp.Value.Matches(content))
                    {
                        if (m.Success && m.Groups.Count >= 3 && controlData.ContainsKey(m.Groups[1].Value))
                        {
                            string propValue = m.Groups[2].Value;

                            if (kvp.Key == "Anchor")
                            {
                                propValue = propValue
                                    .Replace("System.Windows.Forms.AnchorStyles.", "")
                                    .Replace("(", "")
                                    .Replace(")", "");

                                propValue = isCs
                                    ? propValue.Replace("|", ",")
                                    : propValue.Replace("Or", ",");

                                var anchors = propValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(s => s.Trim());
                                propValue = string.Join(", ", anchors);
                            }

                            controlData[m.Groups[1].Value].Properties.Add($"{kvp.Key}: {propValue}");
                        }
                    }
                }

                if (controlData.Count == 0)
                {
                    return "/* No Controls Detected (Form might be empty or parsing failed) */";
                }

                StringBuilder result = new StringBuilder();
                foreach (var ctrl in controlData.OrderBy(c => c.Key))
                {
                    string props = ctrl.Value.Properties.Any()
                        ? $" [{string.Join(" | ", ctrl.Value.Properties)}]"
                        : "";
                    result.AppendLine($"{ctrl.Value.Type}: {ctrl.Key}{props}");
                }

                return result.ToString().Trim();
            }
            catch (RegexMatchTimeoutException)
            {
                return "/* Designer Squish Omitted: File too complex for regex parser (Timeout) */";
            }
            catch (Exception ex)
            {
                return $"/* Designer Squish Error: {ex.Message} */";
            }
        }

        private static string SanitizeCsprojContent(string filePath, string fileContent)
        {
            try
            {
                XDocument xdoc = XDocument.Parse(fileContent);
                if (xdoc.Root == null) return fileContent;

                XNamespace ns = xdoc.Root.GetDefaultNamespace();
                var systemReferencesToRemove = new HashSet<string> {
                    "System", "System.Core", "System.Data", "System.Drawing", "System.Deployment",
                    "System.Net.Http", "System.Windows.Forms", "System.Xml", "System.Xml.Linq"
                };

                xdoc.Descendants(ns + "Reference").Where(r =>
                {
                    var includeAttr = r.Attribute("Include");
                    return includeAttr != null && systemReferencesToRemove.Contains(includeAttr.Value);
                }).Remove();

                xdoc.Descendants(ns + "ItemGroup").Where(ig => !ig.HasElements).Remove();

                return xdoc.ToString(SaveOptions.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sanitizing CSPROJ {filePath}: {ex.Message}");
                return fileContent;
            }
        }

        private static string SanitizeResxContent(string filePath, string fileContent)
        {
            try
            {
                var binaryTypes = new List<string> {
                    "System.Drawing.Bitmap", "System.Drawing.Icon", "System.Resources.ResXFileRef"
                };

                XDocument xdoc = XDocument.Parse(fileContent);
                var dataNodes = xdoc.Descendants("data").ToList();

                dataNodes.Where(node =>
                {
                    var typeAttr = node.Attribute("type");
                    var mimeAttr = node.Attribute("mimetype");

                    bool isBinaryType = typeAttr != null && binaryTypes.Any(bt => typeAttr.Value.StartsWith(bt, StringComparison.OrdinalIgnoreCase));
                    bool isBinaryMime = mimeAttr != null && mimeAttr.Value == "application/x-microsoft.net.object.bytearray.base64";

                    return isBinaryType || isBinaryMime;
                }).Remove();

                return xdoc.Descendants("data").Any() ? xdoc.ToString(SaveOptions.None) : string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sanitizing RESX {filePath}: {ex.Message}");
                return fileContent;
            }
        }

        public static bool IsIgnoredDesigner(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            foreach (var ignore in _designerIgnoreList)
            {
                if (fileName.EndsWith(ignore, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}