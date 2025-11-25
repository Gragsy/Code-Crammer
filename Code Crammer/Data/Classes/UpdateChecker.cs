using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reflection;

namespace Code_Crammer.Data
{
    public static class UpdateChecker
    {
        private const string USER_NAME = "Gragsy";
        private const string REPO_NAME = "Code-Crammer";

        public class UpdateResult
        {
            public bool HasUpdate { get; set; }
            public string? LocalVersion { get; set; }
            public string? RemoteVersion { get; set; }
            public string? DownloadUrl { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public static async Task<UpdateResult> CheckForUpdatesAsync()
        {
            var result = new UpdateResult
            {
                LocalVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0"
            };

            try
            {
                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CodeCrammer", result.LocalVersion));

                    string url = $"https://api.github.com/repos/{USER_NAME}/{REPO_NAME}/releases/latest";
                    var response = await client.GetStringAsync(url);

                    var json = JObject.Parse(response);
                    var tagToken = json["tag_name"];
                    string remoteTag = tagToken?.ToString() ?? string.Empty;

                    if (string.IsNullOrEmpty(remoteTag))
                    {
                        result.ErrorMessage = "Could not retrieve version tag from GitHub.";
                        return result;
                    }

                    string cleanRemoteVersion = remoteTag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                        ? remoteTag.Substring(1)
                        : remoteTag;

                    result.RemoteVersion = cleanRemoteVersion;

                    if (Version.TryParse(cleanRemoteVersion, out Version? remoteVer) &&
                        Version.TryParse(result.LocalVersion, out Version? localVer))
                    {
                        if (remoteVer != null && localVer != null && remoteVer > localVer)
                        {
                            result.HasUpdate = true;
                            result.DownloadUrl = json["html_url"]?.ToString();
                        }
                    }
                    else
                    {
                        result.ErrorMessage = $"Version parsing failed. Local: {result.LocalVersion}, Remote: {cleanRemoteVersion}";
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}