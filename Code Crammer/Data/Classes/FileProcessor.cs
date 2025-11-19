#nullable disable

using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Code_Crammer.Data
{
    public static class FileProcessor
    {
        private static readonly List<string> _designerIgnoreList = new List<string>
        {
            "Resources.Designer.cs", "Settings.Designer.cs", "Reference.cs"
        };

        private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

        private static readonly Regex _csFieldRegex = new Regex(@"(?:private|public|protected|internal)\s+(?:[\w\.]+\.)*(\w+)\s+(\w+);", RegexOptions.Compiled, _regexTimeout);

        private static readonly Dictionary<string, Regex> _csPropertyRegexes = new Dictionary<string, Regex>
        {
            { "Loc", new Regex(@"this\.(\w+)\.Location\s*=\s*new\s*(?:System\.Drawing\.)?Point\s*\(\s*(\d+\s*,\s*\d+)\s*\);", RegexOptions.Compiled, _regexTimeout) },
            { "Size", new Regex(@"this\.(\w+)\.Size\s*=\s*new\s*(?:System\.Drawing\.)?Size\s*\(\s*(\d+\s*,\s*\d+)\s*\);", RegexOptions.Compiled, _regexTimeout) },
            { "Text", new Regex(@"this\.(\w+)\.Text\s*=\s*""((?:\\""|[^""])*)"";", RegexOptions.Compiled | RegexOptions.Singleline, _regexTimeout) },
            { "BackColor", new Regex(@"this\.(\w+)\.BackColor\s*=\s*(?:System\.Drawing\.)?Color\.(\w+);", RegexOptions.Compiled, _regexTimeout) },
            { "Anchor", new Regex(@"this\.(\w+)\.Anchor\s*=\s*\((.*?)\);", RegexOptions.Compiled, _regexTimeout) }
        };

        private static readonly Regex _vbFieldRegex = new Regex(@"(?:Public|Friend|Private|Protected)\s+WithEvents\s+(\w+)\s+As\s+(?:[a-zA-Z_]\w*\.)*(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout);

        private static readonly Dictionary<string, Regex> _vbPropertyRegexes = new Dictionary<string, Regex>
        {
            { "Loc", new Regex(@"(\w+)\.Location\s*=\s*New\s*(?:System\.Drawing\.)?Point\s*\(\s*(\d+\s*,\s*\d+)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout) },
            { "Size", new Regex(@"(\w+)\.Size\s*=\s*New\s*(?:System\.Drawing\.)?Size\s*\(\s*(\d+\s*,\s*\d+)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout) },
            { "Text", new Regex(@"(\w+)\.Text\s*=\s*""((?:""""|[^""])*)""", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, _regexTimeout) },
            { "BackColor", new Regex(@"(\w+)\.BackColor\s*=\s*(?:System\.Drawing\.)?Color\.(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout) },
            { "Anchor", new Regex(@"(\w+)\.Anchor\s*=\s*\((.*?)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout) }
        };

        public static string ProcessFile(string filePath, string content, ScraperOptions options, bool forceDistill)
        {
            string fileName = Path.GetFileName(filePath);
            string fileExt = Path.GetExtension(fileName).ToLowerInvariant();

            bool isDesigner = (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                             fileName.EndsWith(".designer.vb", StringComparison.OrdinalIgnoreCase))
                             && !IsIgnoredDesigner(fileName);

            if (forceDistill && options.DistillUnusedHeaders)
            {
                var lang = LanguageManager.GetProfileForExtension(fileExt);
                string commentChar = (lang != null && lang.Name == "Visual Basic") ? "'" : "//";
                return $"{commentChar} [Content Omitted]";
            }

            if (forceDistill || (options.DistillProject && !isDesigner))
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

            if (isDesigner && options.SquishDesignerFiles)
            {
                if (fileExt == ".cs") return SquishDesignerContent(content, true);
                if (fileExt == ".vb") return SquishDesignerContent(content, false);
            }

            var languageProfile = LanguageManager.GetProfileForExtension(fileExt);
            if (languageProfile != null)
            {
                content = SanitizeCodeContent(content, languageProfile.Name, options);
            }
            else if (options.SanitizeOutput)
            {
                switch (fileExt)
                {
                    case ".resx": return SanitizeResxContent(filePath, content);
                    case ".csproj":
                    case ".vbproj": return SanitizeCsprojContent(filePath, content);
                }
            }

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
                    processed = regex.Replace(processed, m =>
                    {
                        if (m.Groups.Count > 1 && (m.Groups[1].Success || (m.Groups.Count > 2 && m.Groups[2].Success))) return m.Value;
                        return string.Empty;
                    });
                }
            }

            if (options.SanitizeOutput)
            {
                StringBuilder sb = new StringBuilder();
                var lines = processed.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        sb.AppendLine(trimmedLine);
                    }
                }
                return sb.ToString().Trim();
            }

            return processed;
        }

        private static string SanitizeCsprojContent(string filePath, string fileContent)
        {
            try
            {
                XDocument xdoc = XDocument.Parse(fileContent);
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
            catch
            {
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
                    return (typeAttr != null && binaryTypes.Any(bt => typeAttr.Value.StartsWith(bt, StringComparison.OrdinalIgnoreCase))) ||
                           (mimeAttr != null && mimeAttr.Value == "application/x-microsoft.net.object.bytearray.base64");
                }).Remove();

                return xdoc.Descendants("data").Any() ? xdoc.ToString(SaveOptions.None) : string.Empty;
            }
            catch
            {
                return fileContent;
            }
        }

        private static string SquishDesignerContent(string content, bool isCs)
        {
            try
            {
                var controlData = new Dictionary<string, (string Type, List<string> Properties)>(StringComparer.OrdinalIgnoreCase);
                Regex fieldRegex = isCs ? _csFieldRegex : _vbFieldRegex;
                var patterns = isCs ? _csPropertyRegexes : _vbPropertyRegexes;

                foreach (Match m in fieldRegex.Matches(content))
                {
                    if (m.Success && m.Groups.Count >= 3)
                    {
                        if (isCs) controlData[m.Groups[2].Value] = (m.Groups[1].Value, new List<string>());
                        else controlData[m.Groups[1].Value] = (m.Groups[2].Value, new List<string>());
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
                                propValue = isCs
                                    ? propValue.Replace("System.Windows.Forms.AnchorStyles.", "").Replace(" | ", ", ")
                                    : propValue.Replace("System.Windows.Forms.AnchorStyles.", "").Replace(" Or ", ", ");
                            }
                            controlData[m.Groups[1].Value].Properties.Add($"{kvp.Key}: {propValue}");
                        }
                    }
                }

                StringBuilder result = new StringBuilder();
                foreach (var ctrl in controlData.OrderBy(c => c.Key))
                {
                    if (ctrl.Value.Properties.Any())
                    {
                        result.AppendLine($"{ctrl.Value.Type}: {ctrl.Key} [{string.Join(" | ", ctrl.Value.Properties)}]");
                    }
                }

                string final = result.ToString().Trim();
                return string.IsNullOrEmpty(final) ? "/* No Controls Detected or Parsing Failed */" : final;
            }
            catch (RegexMatchTimeoutException)
            {
                return "/* SQUISH FAILED (Timeout): This file is too complex to squish safely. Content omitted. */";
            }
            catch (Exception)
            {
                return $"/* Designer Squishing Failed */\r\n\r\n{content}";
            }
        }

        public static bool IsIgnoredDesigner(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            return _designerIgnoreList.Any(ignore => fileName.EndsWith(ignore, StringComparison.OrdinalIgnoreCase));
        }
    }
}