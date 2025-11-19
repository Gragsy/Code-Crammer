using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;

namespace Code_Crammer.Data
{
    public static class UpdateChecker
    {

        private const string USER_NAME = "Gragsy";
        private const string REPO_NAME = "Code-Crammer";
        public static async Task CheckForUpdatesAsync(bool silentIfUpToDate)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CodeCrammer", "1.0"));

                    string url = $"https://api.github.com/repos/{USER_NAME}/{REPO_NAME}/releases/latest";
                    var response = await client.GetStringAsync(url);

                    var json = JObject.Parse(response);

                    var tagToken = json["tag_name"];
                    string remoteTag = tagToken?.ToString() ?? string.Empty;

                    if (string.IsNullOrEmpty(remoteTag)) return;

                    string cleanVersion = remoteTag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                        ? remoteTag.Substring(1)
                        : remoteTag;

                    Version remoteVer = new Version(cleanVersion);
                    Version localVer = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("1.0.0");

                    if (remoteVer > localVer)
                    {
                        var result = MessageBox.Show(
                            $"A new version ({remoteTag}) is available!\r\n\r\n" +
                            "Would you like to open the download page now?",
                            "Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            var urlToken = json["html_url"];
                            string htmlUrl = urlToken?.ToString() ?? string.Empty;

                            if (!string.IsNullOrEmpty(htmlUrl))
                            {
                                Process.Start(new ProcessStartInfo(htmlUrl) { UseShellExecute = true });
                            }
                        }
                    }
                    else if (!silentIfUpToDate)
                    {
                        MessageBox.Show("You are running the latest version.", "Up to Date", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!silentIfUpToDate)
                {
                    MessageBox.Show($"Could not check for updates: {ex.Message}", "Update Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}