#nullable disable

namespace Code_Crammer.Data
{
    public static class TooltipContent
    {
        private static readonly Dictionary<string, string> _tooltips = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {

            { "Code Files", "Includes source code files (.cs, .vb). The meat and potatoes." },
            { "Config Files", "Includes configuration files (.config, .json settings). Useful for understanding environment setups." },
            { "Designer Files", "Includes Form designer code. Warning: These can be huge and repetitive." },
            { "Include Other Files", "Includes files that don't match standard types (e.g., .txt, .md, .sql)." },
            { "Json Files", "Includes generic JSON files found in the solution." },
            { "Project Files", "Includes the .csproj or .vbproj files themselves. Good for seeing dependencies." },
            { "Resource Files", "Includes .resx files. Usually just strings or image references." },

            { "Distill Active Projects Only", "Only includes headers for projects that are actually checked in the tree. Ignores the rest." },
            { "Distill Project (Bible Mode)", "Creates a high-level summary (signatures only). No implementation code. Perfect for giving an AI the 'map' of the project." },
            { "Distill Unused", "If a file is NOT checked, it will still be included but only as a header/summary. Checked files remain full code." },
            { "Exclude Project Settings", "Ignores the 'My Project' or 'Properties' folders. Keeps the output focused on logic." },
            { "Remove Comments", "Strips out all code comments to save tokens. Ruthless efficiency." },
            { "Sanitize Files (Recommended)", "Removes binary data from RESX and standardizes whitespace. Highly recommended." },
            { "Squish Designer Files", "Compresses designer code into a single-line summary of controls and properties. Saves massive amounts of space." },

            { "Copy To Clipboard", "Automatically copies the result to your clipboard when done." },
            { "Create File", "Saves the result as a text file in your Downloads folder." },
            { "Include Message", "Appends the custom message (from the 'Edit Message' button) to the end of the output." },
            { "Include Project Structure", "Adds a visual tree diagram of the folder structure at the top of the file." },
            { "Open File On Completion", "Opens the generated text file immediately after creation." },
            { "Open Folder On Completion", "Opens the folder containing the generated file." },
            { "Show Per-File Token Counts", "Displays the approximate token cost next to every file in the tree view." },

            { "btnSelectFolder", "Pick the root folder of the solution you want to cram." },
            { "btnGenerate", "Start the scraping process." },
            { "btnDefault", "Reset all settings to their factory defaults." },
            { "btnEditMessage", "Open the editor to add a custom prompt or instruction to the output." },
            { "txtFolderPath", "The currently selected solution path." },
            { "lblTokenCount", "Estimated token count based on current selection. (1 Token ~= 4 Characters)" }
        };

        public static string GetTooltip(string key)
        {
            return _tooltips.TryGetValue(key, out string value) ? value : null;
        }
    }
}