#nullable enable

#nullable enable

using Code_Crammer.Data.Classes.Services;
using Code_Crammer.Data.Classes.Skin;

namespace Code_Crammer.Data.Classes.Components
{
    public class ProfileUiManager
    {
        public void PopulateHistoryMenu(ToolStripDropDownButton btnHistory, EventHandler clickHandler, MouseEventHandler mouseDownHandler)
        {
            btnHistory.DropDownItems.Clear();
            string historyFolder = PathManager.GetHistoryFolderPath();
            if (Directory.Exists(historyFolder))
            {
                var files = new DirectoryInfo(historyFolder).GetFiles("*.json")
                    .OrderByDescending(f => f.LastWriteTime);

                string currentSkin = SettingsManager.GetAppSkin();
                bool isDark = currentSkin == "Dark";
                var skinEnum = isDark ? SkinManager.Skin.Dark : SkinManager.Skin.Light;

                foreach (var file in files)
                {
                    string displayText = Path.GetFileNameWithoutExtension(file.Name);
                    var item = new ToolStripMenuItem(displayText);
                    item.Tag = file.FullName;
                    item.Click += clickHandler;
                    item.MouseDown += mouseDownHandler;
                    item.ToolTipText = "Click to restore. Right-click to delete.";
                    item.ForeColor = isDark ? Color.FromArgb(241, 241, 241) : SystemColors.ControlText;
                    item.BackColor = isDark ? Color.FromArgb(27, 27, 28) : SystemColors.Control;
                    btnHistory.DropDownItems.Add(item);
                }
            }
            btnHistory.Enabled = btnHistory.DropDownItems.Count > 0;
        }

        public void PopulateProfilesMenu(ToolStripDropDownButton btnLoad, EventHandler profileClickHandler, EventHandler browseClickHandler)
        {
            btnLoad.DropDownItems.Clear();

            var browseItem = new ToolStripMenuItem("Browse for Profile...");
            browseItem.Click += browseClickHandler;
            btnLoad.DropDownItems.Add(browseItem);
            btnLoad.DropDownItems.Add(new ToolStripSeparator());

            string profilesPath = PathManager.GetProfilesFolderPath();
            if (Directory.Exists(profilesPath))
            {
                var files = Directory.GetFiles(profilesPath, "*.json");
                foreach (var file in files)
                {
                    string profileName = Path.GetFileNameWithoutExtension(file);
                    var item = new ToolStripMenuItem(profileName);
                    item.Tag = file;
                    item.Click += profileClickHandler;
                    btnLoad.DropDownItems.Add(item);
                }
            }
        }

        public string? ShowSaveDialog()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.InitialDirectory = PathManager.GetProfilesFolderPath();
                dialog.Filter = "Profile Files (*.json)|*.json|All files (*.*)|*.*";
                dialog.Title = "Save Scraper Profile As...";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }

        public string? ShowOpenDialog()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = PathManager.GetProfilesFolderPath();
                dialog.Filter = "Profile Files (*.json)|*.json|All files (*.*)|*.*";
                dialog.Title = "Load Scraper Profile";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }

        public void DeleteHistoryItem(ToolStripMenuItem? item, Action<string, Color> logAction, ToolStripDropDownButton parentBtn)
        {
            if (item == null) return;
            string? filePath = item.Tag?.ToString();

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    parentBtn.DropDownItems.Remove(item);
                    logAction($"Deleted history item: {item.Text}", Color.Gray);

                    if (parentBtn.DropDownItems.Count == 0)
                    {
                        parentBtn.Enabled = false;
                    }
                }
                catch (Exception ex)
                {
                    logAction($"Failed to delete history item: {ex.Message}", Color.Red);
                }
            }
        }
    }
}