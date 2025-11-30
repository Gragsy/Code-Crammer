#nullable enable

using Code_Crammer.Data.Classes.Models;
using Code_Crammer.Data.Classes.Skin;
using System.ComponentModel;

namespace Code_Crammer.Data.Classes.Components
{
    public class OptionsUiManager
    {
        private readonly CheckedListBox _clbFileTypes;
        private readonly CheckedListBox _clbProcessing;
        private readonly CheckedListBox _clbOutput;
        private readonly Action<string, Color> _log;
        private int _lastTooltipIndex = -1;

        public OptionsUiManager(CheckedListBox fileTypes, CheckedListBox processing, CheckedListBox output, Action<string, Color> logAction)
        {
            _clbFileTypes = fileTypes;
            _clbProcessing = processing;
            _clbOutput = output;
            _log = logAction;
        }

        public void BindEvents(ItemCheckEventHandler itemCheckHandler, ToolTip tipTop)
        {
            _clbFileTypes.ItemCheck += itemCheckHandler;
            _clbProcessing.ItemCheck += itemCheckHandler;
            _clbOutput.ItemCheck += itemCheckHandler;

            _clbFileTypes.MouseMove += (s, e) => HandleListTooltip(_clbFileTypes, e, tipTop);
            _clbProcessing.MouseMove += (s, e) => HandleListTooltip(_clbProcessing, e, tipTop);
            _clbOutput.MouseMove += (s, e) => HandleListTooltip(_clbOutput, e, tipTop);
        }

        public void UnbindEvents(ItemCheckEventHandler itemCheckHandler)
        {
            _clbFileTypes.ItemCheck -= itemCheckHandler;
            _clbProcessing.ItemCheck -= itemCheckHandler;
            _clbOutput.ItemCheck -= itemCheckHandler;
        }

        public void SetControlsEnabled(bool enabled)
        {
            _clbFileTypes.Enabled = enabled;
            _clbProcessing.Enabled = enabled;
            _clbOutput.Enabled = enabled;
        }

        public ScraperOptions GetCurrentOptions(bool showTokens)
        {
            return new ScraperOptions
            {
                IncludeCode = IsOptionChecked(ScraperOption.CodeFiles),
                IncludeProjectFile = IsOptionChecked(ScraperOption.ProjectFiles),
                IncludeResx = IsOptionChecked(ScraperOption.ResourceFiles),
                IncludeFolderLayout = IsOptionChecked(ScraperOption.IncludeProjectStructure),
                IncludeConfig = IsOptionChecked(ScraperOption.ConfigFiles),
                IncludeJson = IsOptionChecked(ScraperOption.JsonFiles),
                IncludeDesigner = IsOptionChecked(ScraperOption.DesignerFiles),
                SquishDesignerFiles = IsOptionChecked(ScraperOption.SquishDesignerFiles),
                SanitizeOutput = IsOptionChecked(ScraperOption.SanitizeFiles),
                RemoveComments = IsOptionChecked(ScraperOption.RemoveComments),
                CreateFile = IsOptionChecked(ScraperOption.CreateFile),
                OpenFolderOnCompletion = IsOptionChecked(ScraperOption.OpenFolderOnCompletion),
                OpenFileOnCompletion = IsOptionChecked(ScraperOption.OpenFileOnCompletion),
                CopyToClipboard = IsOptionChecked(ScraperOption.CopyToClipboard),
                IncludeMessage = IsOptionChecked(ScraperOption.IncludeMessage),
                IncludeOtherFiles = IsOptionChecked(ScraperOption.IncludeOtherFiles),
                DistillProject = IsOptionChecked(ScraperOption.DistillProject),
                DistillUnused = IsOptionChecked(ScraperOption.DistillUnused),
                DistillUnusedHeaders = IsOptionChecked(ScraperOption.DistillUnusedHeaders),
                ShowPerFileTokens = showTokens,
                ExcludeProjectSettings = IsOptionChecked(ScraperOption.ExcludeMyProject)
            };
        }

        public void ApplyOptionsToUI(ScraperOptions options)
        {
            SetOptionChecked(ScraperOption.IncludeProjectStructure, options.IncludeFolderLayout);
            SetOptionChecked(ScraperOption.CodeFiles, options.IncludeCode);
            SetOptionChecked(ScraperOption.ConfigFiles, options.IncludeConfig);
            SetOptionChecked(ScraperOption.DesignerFiles, options.IncludeDesigner);
            SetOptionChecked(ScraperOption.SquishDesignerFiles, options.SquishDesignerFiles);
            SetOptionChecked(ScraperOption.JsonFiles, options.IncludeJson);
            SetOptionChecked(ScraperOption.ProjectFiles, options.IncludeProjectFile);
            SetOptionChecked(ScraperOption.ResourceFiles, options.IncludeResx);
            SetOptionChecked(ScraperOption.SanitizeFiles, options.SanitizeOutput);
            SetOptionChecked(ScraperOption.RemoveComments, options.RemoveComments);
            SetOptionChecked(ScraperOption.CreateFile, options.CreateFile);
            SetOptionChecked(ScraperOption.OpenFileOnCompletion, options.OpenFileOnCompletion);
            SetOptionChecked(ScraperOption.OpenFolderOnCompletion, options.OpenFolderOnCompletion);
            SetOptionChecked(ScraperOption.CopyToClipboard, options.CopyToClipboard);
            SetOptionChecked(ScraperOption.IncludeMessage, options.IncludeMessage);
            SetOptionChecked(ScraperOption.ExcludeMyProject, options.ExcludeProjectSettings);
            SetOptionChecked(ScraperOption.DistillProject, options.DistillProject);
            SetOptionChecked(ScraperOption.DistillUnused, options.DistillUnused);
            SetOptionChecked(ScraperOption.DistillUnusedHeaders, options.DistillUnusedHeaders);
        }

        public Dictionary<string, bool> CaptureState()
        {
            var options = new Dictionary<string, bool>();
            foreach (CheckedListBox box in new[] { _clbFileTypes, _clbProcessing, _clbOutput })
            {
                for (int i = 0; i < box.Items.Count; i++)
                {
                    string key = box.Items[i].ToString() ?? string.Empty;
                    bool value = box.GetItemChecked(i);
                    options[key] = value;
                }
            }
            return options;
        }

        public void RestoreState(Dictionary<string, bool> options)
        {
            foreach (CheckedListBox box in new[] { _clbFileTypes, _clbProcessing, _clbOutput })
            {
                for (int i = 0; i < box.Items.Count; i++)
                {
                    string key = box.Items[i].ToString() ?? string.Empty;
                    if (options.ContainsKey(key))
                    {
                        bool shouldBeChecked = options[key];
                        if (box.GetItemChecked(i) != shouldBeChecked)
                        {
                            box.SetItemChecked(i, shouldBeChecked);
                        }
                    }
                }
            }
        }

        public void ResetGroupToDefaults(CheckedListBox clb)
        {
            if (clb == null) return;
            var defaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (clb == _clbFileTypes)
            {
                defaults["Code Files"] = true;
                defaults["Config Files"] = false;
                defaults["Designer Files"] = false;
                defaults["Include Other Files"] = false;
                defaults["Json Files"] = false;
                defaults["Project Files"] = false;
                defaults["Resource Files"] = false;
            }
            else if (clb == _clbProcessing)
            {
                defaults["Distill Active Projects Only"] = false;
                defaults["Distill Project (Bible Mode)"] = false;
                defaults["Distill Unused"] = false;
                defaults["Exclude Project Settings"] = true;
                defaults["Remove Comments"] = true;
                defaults["Sanitize Files (Recommended)"] = true;
                defaults["Squish Designer Files"] = false;
            }
            else if (clb == _clbOutput)
            {
                defaults["Copy To Clipboard"] = true;
                defaults["Create File"] = false;
                defaults["Include Message"] = true;
                defaults["Include Project Structure"] = true;
                defaults["Open File On Completion"] = false;
                defaults["Open Folder On Completion"] = false;
            }

            for (int i = 0; i < clb.Items.Count; i++)
            {
                string key = clb.Items[i].ToString() ?? string.Empty;
                if (defaults.ContainsKey(key))
                {
                    clb.SetItemChecked(i, defaults[key]);
                }
            }
            _log($"Reset options for '{clb.Parent?.Text ?? "Group"}' to defaults.", Color.Gray);
        }

        private void SetOptionChecked(ScraperOption optionValue, bool isChecked)
        {
            string optionText = GetEnumDescription(optionValue);
            foreach (CheckedListBox box in new[] { _clbFileTypes, _clbProcessing, _clbOutput })
            {
                int index = box.Items.IndexOf(optionText);
                if (index != -1)
                {
                    box.SetItemChecked(index, isChecked);
                    return;
                }
            }
        }

        private bool IsOptionChecked(ScraperOption optionValue)
        {
            string optionText = GetEnumDescription(optionValue);
            foreach (CheckedListBox box in new[] { _clbFileTypes, _clbProcessing, _clbOutput })
            {
                int itemIndex = box.Items.IndexOf(optionText);
                if (itemIndex != -1)
                {
                    return box.GetItemChecked(itemIndex);
                }
            }
            _log($"Could not find option '{optionText}' in the lists.", Color.Red);
            return false;
        }

        private string GetEnumDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return value.ToString();
            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        private void HandleListTooltip(CheckedListBox list, MouseEventArgs e, ToolTip tipTop)
        {
            if (!tipTop.Active)
            {
                if (_lastTooltipIndex != -1)
                {
                    tipTop.SetToolTip(list, null);
                    _lastTooltipIndex = -1;
                }
                return;
            }
            int index = list.IndexFromPoint(e.Location);
            if (index != _lastTooltipIndex)
            {
                _lastTooltipIndex = index;
                if (index >= 0)
                {
                    string itemText = list.Items[index].ToString() ?? string.Empty;
                    string? tip = TooltipContent.GetTooltip(itemText);
                    tipTop.SetToolTip(list, tip);
                }
                else
                {
                    tipTop.SetToolTip(list, null);
                }
            }
        }
    }
}