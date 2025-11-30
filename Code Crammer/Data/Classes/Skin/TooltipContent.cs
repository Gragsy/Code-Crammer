#nullable enable

namespace Code_Crammer.Data.Classes.Skin
{
    public static class TooltipContent
    {
        private static readonly Dictionary<string, string> _tooltips = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Code Files", "Includes source code files (.cs, .vb). The meat and potatoes." },
            { "Config Files", "Includes configuration files (.config, .json settings)." },
            { "Designer Files", "Includes Form designer code. Warning: These can be huge." },
            { "Include Other Files", "Includes files that don't match standard types (e.g., .txt, .md, .sql)." },
            { "Json Files", "Includes generic JSON files." },
            { "Project Files", "Includes .csproj or .vbproj files." },
            { "Resource Files", "Includes .resx files." },
            { "Distill Active Projects Only", "Only includes headers for projects that are actually checked in the tree." },
            { "Distill Project (Bible Mode)", "Creates a high-level summary (signatures only). No implementation code." },
            { "Distill Unused", "Unchecked files are included as headers/summaries only." },
            { "Exclude Project Settings", "Ignores 'My Project' or 'Properties' folders." },
            { "Remove Comments", "Strips out all code comments." },
            { "Sanitize Files (Recommended)", "Removes binary data from RESX, trims whitespace, and fixes indentation." },
            { "Squish Designer Files", "Compresses designer code into a single-line summary of controls." },
            { "Copy To Clipboard", "Copies the result to your clipboard." },
            { "Create File", "Saves the result as a text file in Downloads." },
            { "Include Message", "Appends your custom message." },
            { "Include Project Structure", "Adds a visual tree diagram of the folder structure." },
            { "Open File On Completion", "Opens the generated text file." },
            { "Open Folder On Completion", "Opens the folder containing the generated file." },
            { "Show Per-File Token Counts", "Displays approx. token cost next to every file." },
            { "btnSelectFolder", "Pick the root folder of the solution." },
            { "btnGenerate", "Start the cramming process." },
            { "btnDefault", "Reset all settings to factory defaults." },
            { "btnEditMessage", "Add a custom instruction to the AI." },
            { "txtFolderPath", "The currently selected solution path." },
            { "lblTokenCount", "Estimated token count. (1 Token ~= 4 Characters)." },
            { "btnHistory", "Load previous sessions. Right-click an item to delete it." },
            { "btnUndo", "Undo the last file check or option change." },
            { "btnRedo", "Redo the last undone action." },
            { "btnLoad", "Load a saved profile (.json)." },
            { "btnSave", "Save current settings as a profile." },
            { "btnSaveAs", "Save current settings as a new profile file." },
            { "btnHelp", "View About and Update information." },
            { "txtSearch", "Type to filter the file tree. Press Enter to search." },
            { "mnuView", "Open the selected file in Notepad." },
            { "mnuExplorer", "Opens the selected file or folder in Windows Explorer." },
            { "mnuConvertToText", "Exports the current TreeView structure to a text file." },
            { "mnuCram", "Instantly cram the selected file or folder using current settings." },
            { "mnuDeleteHistoryItem", "Permanently deletes this history snapshot." },
            { "mnuResetToDefault", "Resets the options in this list to their factory defaults." }
        };

        public static string? GetTooltip(string key)
        {
            return _tooltips.TryGetValue(key, out string? value) ? value : null;
        }
    }
}