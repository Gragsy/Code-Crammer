#nullable enable

using Code_Crammer.Data.Classes.Core;
using Code_Crammer.Data.Classes.Models;
using Code_Crammer.Data.Classes.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Code_Crammer.Data.Classes.Components
{
    public class ContextMenuHandler
    {
        private readonly Action<string, Color> _log;
        private readonly Func<ScraperOptions> _getOptions;
        private readonly TreeViewManager _treeViewManager;

        public ContextMenuHandler(Action<string, Color> logAction, Func<ScraperOptions> getOptionsFunc, TreeViewManager treeManager)
        {
            _log = logAction;
            _getOptions = getOptionsFunc;
            _treeViewManager = treeManager;
        }

        public void HandleTreeViewOpening(TreeView tvwFiles, ToolStripMenuItem mnuView, CancelEventArgs e)
        {
            if (tvwFiles.SelectedNode == null)
            {
                e.Cancel = true;
                return;
            }
            bool isFile = tvwFiles.SelectedNode.Tag is string tagStr && Path.HasExtension(tagStr);
            mnuView.Enabled = isFile;
        }

        public void ViewSelectedFile(TreeNode? selectedNode, string solutionPath)
        {
            if (selectedNode?.Tag is not string relativePath || string.IsNullOrEmpty(relativePath)) return;

            string fullPath = Path.GetFullPath(Path.Combine(solutionPath, relativePath));
            string safeCheck = PathHelper.GetSafeRelativePath(solutionPath, fullPath);

            if (string.IsNullOrEmpty(safeCheck))
            {
                _log("Security Alert: Attempted to access file outside solution directory.", Color.Red);
                return;
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = $"\"{fullPath}\"",
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                    _log($"Opening '{relativePath}' in Notepad.", Color.LimeGreen);
                }
                else
                {
                    _log($"Cannot open file: '{fullPath}' not found.", Color.Red);
                }
            }
            catch (Exception ex)
            {
                _log($"Error opening file: {ex.Message}", Color.Red);
            }
        }

        public void ExploreSelectedItem(TreeNode? selectedNode, string solutionPath)
        {
            if (selectedNode?.Tag is not string relativePath || string.IsNullOrEmpty(relativePath)) return;
            string fullPath = Path.GetFullPath(Path.Combine(solutionPath, relativePath));
            string safeCheck = PathHelper.GetSafeRelativePath(solutionPath, fullPath);
            if (string.IsNullOrEmpty(safeCheck))
            {
                _log("Security Alert: Attempted to access path outside solution.", Color.Red);
                return;
            }
            try
            {
                if (File.Exists(fullPath))
                {
                    Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                }
                else if (Directory.Exists(fullPath))
                {
                    Process.Start("explorer.exe", $"\"{fullPath}\"");
                }
                else
                {
                    _log($"Cannot open Explorer: Path not found '{fullPath}'", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                _log($"Error opening Explorer: {ex.Message}", Color.Red);
            }
        }

        public void ConvertToText()
        {
            try
            {
                _log("Generating TreeView text file...", Color.Yellow);
                string treeText = _treeViewManager.GetTreeAsText();
                string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".txt");
                File.WriteAllText(tempFilePath, treeText);
                Process.Start("notepad.exe", tempFilePath);
                _log("TreeView structure opened in Notepad.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                _log($"Error converting tree to text: {ex.Message}", Color.Red);
            }
        }

        public void CollapseUnusedNodes(TreeView tvwFiles)
        {
            tvwFiles.BeginUpdate();
            try
            {
                foreach (TreeNode node in tvwFiles.Nodes)
                {
                    if (!node.Checked)
                    {
                        node.Collapse();
                    }
                }
                _log("Collapsed all unused projects.", Color.LimeGreen);
            }
            finally
            {
                tvwFiles.EndUpdate();
            }
        }

        public void OpenSelectedFolder(TreeNode? selectedNode, string solutionPath)
        {
            if (selectedNode == null) return;
            string path = solutionPath;

            if (selectedNode.Tag is string relativePath && !string.IsNullOrEmpty(relativePath))
            {
                string fullPath = Path.Combine(solutionPath, relativePath);
                if (File.Exists(fullPath))
                {
                    path = Path.GetDirectoryName(fullPath) ?? solutionPath;
                }
                else if (Directory.Exists(fullPath))
                {
                    path = fullPath;
                }
            }

            try
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
            }
            catch (Exception ex)
            {
                _log($"Could not open folder: {ex.Message}", Color.Red);
            }
        }

        public async Task CramSelectionAsync(TreeNode? selectedNode, string solutionPath, Cursor originalCursor, Action<Cursor> setCursor)
        {
            if (selectedNode?.Tag is not string relativePath || string.IsNullOrEmpty(relativePath)) return;

            string fullPath = Path.Combine(solutionPath, relativePath);
            if (string.IsNullOrEmpty(PathHelper.GetSafeRelativePath(solutionPath, fullPath)))
            {
                _log("Security Alert: Attempted to access path outside solution.", Color.Red);
                return;
            }

            bool isFile = File.Exists(fullPath);
            bool isDir = Directory.Exists(fullPath);

            if (!isFile && !isDir) return;

            try
            {
                setCursor(Cursors.WaitCursor);
                _log($"Cramming {(isFile ? "file" : "folder")}: {relativePath}...", Color.Yellow);

                var options = _getOptions();

                string result = await Task.Run(() =>
                {
                    var cramOptions = new ScraperOptions
                    {
                        IncludeCode = options.IncludeCode,
                        IncludeDesigner = options.IncludeDesigner,
                        SquishDesignerFiles = options.SquishDesignerFiles,
                        SanitizeOutput = options.SanitizeOutput,
                        RemoveComments = options.RemoveComments
                    };

                    StringBuilder finalOutput = new StringBuilder();

                    if (isFile)
                    {
                        try
                        {
                            string content = File.ReadAllText(fullPath);
                            finalOutput.Append(FileProcessor.ProcessFile(fullPath, content, cramOptions, false));
                        }
                        catch (Exception ex)
                        {

                            return $"Error reading file: {ex.Message}";
                        }
                    }
                    else
                    {
                        var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            if (PathHelper.IsPathIgnored(file)) continue;

                            if (FileInclusionHelper.ShouldIncludeFile(file, options, out _))
                            {

                                try
                                {
                                    finalOutput.AppendLine($"{AppConstants.HeaderFilePrefix}{PathHelper.GetSafeRelativePath(solutionPath, file)} ---");
                                    string content = File.ReadAllText(file);
                                    finalOutput.AppendLine(FileProcessor.ProcessFile(file, content, cramOptions, false));
                                    finalOutput.AppendLine(new string('-', 30));
                                    finalOutput.AppendLine();
                                }
                                catch (Exception ex)
                                {

                                    finalOutput.AppendLine($"[ERROR: Could not read file {Path.GetFileName(file)}: {ex.Message}]");
                                    finalOutput.AppendLine(new string('-', 30));
                                }
                            }
                        }
                    }
                    return finalOutput.ToString();
                });

                if (result.StartsWith("Error reading file"))
                {
                    _log(result, Color.Red);
                    return;
                }

                if (result.Length == 0)
                {
                    _log("Nothing to cram (check your File Type settings).", Color.Orange);
                    return;
                }

                string tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".txt");
                await File.WriteAllTextAsync(tempFile, result);
                Process.Start("notepad.exe", tempFile);
                _log("Cram successful.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                _log($"Error cramming: {ex.Message}", Color.Red);
            }
            finally
            {
                setCursor(originalCursor);
            }
        }
    }
}