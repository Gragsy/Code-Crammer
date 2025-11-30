#nullable enable

using Code_Crammer.Data.Classes.Models;
using Newtonsoft.Json;

namespace Code_Crammer.Data.Classes.Services
{
    public static class SettingsManager
    {
        public static async Task SaveSettingsAsync(ScraperOptions options, string folderPath, SessionState sessionState)
        {
            await Task.Run(() =>
            {
                var settings = Properties.Settings.Default;

                settings.LastFolderPath = folderPath;
                settings.IncludeProjectStructure = options.IncludeFolderLayout;
                settings.IncludeCodeFiles = options.IncludeCode;
                settings.IncludeConfigFiles = options.IncludeConfig;
                settings.IncludeDesignerFiles = options.IncludeDesigner;
                settings.SquishDesignerFiles = options.SquishDesignerFiles;
                settings.IncludeJsonFiles = options.IncludeJson;
                settings.IncludeProjectFiles = options.IncludeProjectFile;
                settings.IncludeResourceFiles = options.IncludeResx;
                settings.SanitizeFiles = options.SanitizeOutput;
                settings.RemoveComments = options.RemoveComments;
                settings.CreateFile = options.CreateFile;
                settings.OpenFolderOnFinish = options.OpenFolderOnCompletion;
                settings.OpenFileAfterFinish = options.OpenFileOnCompletion;
                settings.CopyToClipboard = options.CopyToClipboard;
                settings.IncludeFancyMessage = options.IncludeMessage;
                settings.ExcludeMyProject = options.ExcludeProjectSettings;
                settings.DistillProject = options.DistillProject;
                settings.DistillUnused = options.DistillUnused;
                settings.DistillUnusedHeaders = options.DistillUnusedHeaders;
                settings.ShowPerFileTokens = options.ShowPerFileTokens;

                if (sessionState != null)
                {
                    settings.LastSessionStateJson = JsonConvert.SerializeObject(sessionState);
                }

                settings.Save();
            });
        }

        public static (ScraperOptions Options, string LastPath, SessionState Session) LoadSettings()
        {
            var settings = Properties.Settings.Default;
            var options = new ScraperOptions();

            string lastPath = settings.LastFolderPath;
            if (string.Equals(lastPath, @"C:\", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lastPath, @"C:", StringComparison.OrdinalIgnoreCase))
            {
                lastPath = string.Empty;
            }

            options.IncludeFolderLayout = settings.IncludeProjectStructure;
            options.IncludeCode = settings.IncludeCodeFiles;
            options.IncludeConfig = settings.IncludeConfigFiles;
            options.IncludeDesigner = settings.IncludeDesignerFiles;
            options.SquishDesignerFiles = settings.SquishDesignerFiles;
            options.IncludeJson = settings.IncludeJsonFiles;
            options.IncludeProjectFile = settings.IncludeProjectFiles;
            options.IncludeResx = settings.IncludeResourceFiles;
            options.SanitizeOutput = settings.SanitizeFiles;
            options.RemoveComments = settings.RemoveComments;
            options.CreateFile = settings.CreateFile;
            options.OpenFolderOnCompletion = settings.OpenFolderOnFinish;
            options.OpenFileOnCompletion = settings.OpenFileAfterFinish;
            options.CopyToClipboard = settings.CopyToClipboard;
            options.IncludeMessage = settings.IncludeFancyMessage;
            options.ExcludeProjectSettings = settings.ExcludeMyProject;
            options.DistillProject = settings.DistillProject;
            options.DistillUnused = settings.DistillUnused;
            options.DistillUnusedHeaders = settings.DistillUnusedHeaders;
            options.ShowPerFileTokens = settings.ShowPerFileTokens;
            options.IncludeOtherFiles = false;

            SessionState? session = null;
            if (!string.IsNullOrEmpty(settings.LastSessionStateJson))
            {
                try
                {
                    session = JsonConvert.DeserializeObject<SessionState>(settings.LastSessionStateJson);
                }
                catch
                {
                }
            }

            if (session == null)
            {
                session = new SessionState();
            }

            return (options, lastPath, session);
        }

        public static string GetAppSkin()
        {
            return string.IsNullOrEmpty(Properties.Settings.Default.AppSkin) ? "Light" : Properties.Settings.Default.AppSkin;
        }

        public static void SaveAppSkin(string skin)
        {
            Properties.Settings.Default.AppSkin = skin;
            Properties.Settings.Default.Save();
        }

        public static bool GetShowToolTips() => Properties.Settings.Default.ShowToolTips;

        public static void SaveShowToolTips(bool show)
        {
            Properties.Settings.Default.ShowToolTips = show;
            Properties.Settings.Default.Save();
        }

        public static float GetMessageEditorZoom()
        {
            try
            {
                return (float)Properties.Settings.Default["MessageEditorZoom"];
            }
            catch
            {
                return 1.0f;
            }
        }

        public static void SaveMessageEditorZoom(float zoom)
        {
            try
            {
                Properties.Settings.Default["MessageEditorZoom"] = zoom;
                Properties.Settings.Default.Save();
            }
            catch { }
        }
    }
}