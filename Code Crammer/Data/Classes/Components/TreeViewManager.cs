#nullable enable
using Code_Crammer.Data.Classes.Core;
using Code_Crammer.Data.Classes.Models;
using Code_Crammer.Data.Classes.Utilities;
using System.Text;

namespace Code_Crammer.Data.Classes.Components
{
    public class TreeViewManager
    {
        private readonly TreeView _treeView;
        private readonly Action<string, Color> _logAction;

        public TreeViewManager(TreeView treeView, Action<string, Color> logAction)
        {
            _treeView = treeView;
            _logAction = logAction;
        }

        public void UpdateParentStateFromNode(TreeNode? node)
        {
            if (node == null) return;
            UpdateParentNodeCheckState(node.Parent);
        }

        public async Task<List<string>> PopulateFileTreeAsync(string solutionPath, ScraperOptions options, bool excludeMyProject, HashSet<string>? lastCheckedFiles, HashSet<string>? lastExpandedNodes)
        {
            _treeView.BeginUpdate();
            _treeView.Nodes.Clear();

            try
            {

                var (files, projectFiles) = await Task.Run(() =>
                {
                    var allFiles = PathHelper.SafeGetFiles(solutionPath);
                    var projFiles = new List<string>();
                    var filteredFiles = new List<string>();

                    foreach (var file in allFiles)
                    {
                        if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                            file.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
                        {
                            projFiles.Add(file);
                        }

                        if (FileInclusionHelper.ShouldIncludeFile(file, options, out _))
                        {
                            filteredFiles.Add(file);
                        }
                        else if (IsSolutionItem(file, options))
                        {
                            filteredFiles.Add(file);
                        }
                    }
                    return (filteredFiles, projFiles);
                });

                PopulateNodesDirectly(solutionPath, files, options, excludeMyProject, lastCheckedFiles, lastExpandedNodes);

                return projectFiles;
            }
            finally
            {
                _treeView.EndUpdate();
            }
        }

        private void PopulateNodesDirectly(
                    string solutionPath,
                    List<string> files,
                    ScraperOptions options,
                    bool excludeMyProject,
                    HashSet<string>? lastCheckedFiles,
                    HashSet<string>? lastExpandedNodes)
        {
            var nodeCache = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

            string rootName = new DirectoryInfo(solutionPath).Name;
            TreeNode rootNode = new TreeNode(rootName) { Tag = solutionPath };

            if (lastExpandedNodes != null && lastExpandedNodes.Contains(solutionPath))
            {
                rootNode.Expand();
            }
            else if (lastExpandedNodes == null)
            {

                rootNode.Expand();
            }

            _treeView.Nodes.Add(rootNode);
            nodeCache[solutionPath] = rootNode;

            foreach (var filePath in files.OrderBy(f => f))
            {
                string dirPath = Path.GetDirectoryName(filePath) ?? solutionPath;

                if (excludeMyProject)
                {
                    if (filePath.Contains($"{Path.DirectorySeparatorChar}My Project{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                        filePath.Contains($"{Path.DirectorySeparatorChar}Properties{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                TreeNode parentNode = EnsureDirectoryNodes(dirPath, solutionPath, nodeCache, rootNode, lastExpandedNodes);

                var fileNode = CreateFileNode(filePath, solutionPath, options);

                string? relPath = fileNode.Tag as string;

                if (relPath != null)
                {

                    if (lastCheckedFiles != null)
                    {
                        fileNode.Checked = lastCheckedFiles.Contains(relPath);
                    }

                    else if (FileInclusionHelper.ShouldIncludeFile(filePath, options, out bool defaultCheck))
                    {
                        fileNode.Checked = defaultCheck;
                    }
                }

                parentNode.Nodes.Add(fileNode);
            }

            UpdateParentChecksRecursive(rootNode);
        }

        private void UpdateParentChecksRecursive(TreeNode node)
        {
            bool anyChildChecked = false;
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Nodes.Count > 0)
                {
                    UpdateParentChecksRecursive(child);
                }
                if (child.Checked) anyChildChecked = true;
            }

            if (node.Checked != anyChildChecked) node.Checked = anyChildChecked;

            if (node.Checked)
            {
                node.Expand();
            }
        }

        public void RestoreTreeState(HashSet<string>? checkedFiles)
        {
            if (checkedFiles == null) return;

            _treeView.BeginUpdate();
            try
            {
                void RecursiveSet(TreeNodeCollection nodes)
                {
                    foreach (TreeNode node in nodes)
                    {

                        if (node.Tag is string tagStr && Path.HasExtension(tagStr))
                        {
                            node.Checked = checkedFiles.Contains(tagStr);
                        }

                        if (node.Nodes.Count > 0)
                        {
                            RecursiveSet(node.Nodes);

                            bool hasCheckedChild = node.Nodes.Cast<TreeNode>().Any(n => n.Checked);
                            node.Checked = hasCheckedChild;

                            if (hasCheckedChild)
                            {
                                node.Expand();
                            }
                            else
                            {
                                node.Collapse();
                            }
                        }
                    }
                }
                RecursiveSet(_treeView.Nodes);
            }
            finally
            {
                _treeView.EndUpdate();
            }
        }

        private TreeNode EnsureDirectoryNodes(
            string dirPath,
            string solutionPath,
            Dictionary<string, TreeNode> nodeCache,
            TreeNode rootNode,
            HashSet<string>? lastExpandedNodes)
        {
            if (nodeCache.TryGetValue(dirPath, out var existingNode))
            {
                return existingNode;
            }

            if (dirPath.Length <= solutionPath.Length)
            {
                return rootNode;
            }

            string parentDir = Path.GetDirectoryName(dirPath) ?? solutionPath;
            TreeNode parentNode = EnsureDirectoryNodes(parentDir, solutionPath, nodeCache, rootNode, lastExpandedNodes);

            string dirName = Path.GetFileName(dirPath);
            string relPath = PathHelper.GetSafeRelativePath(solutionPath, dirPath);

            TreeNode newDirNode = new TreeNode(dirName)
            {
                Tag = relPath
            };

            if (lastExpandedNodes != null && lastExpandedNodes.Contains(relPath))
            {
                newDirNode.Expand();
            }

            parentNode.Nodes.Add(newDirNode);
            nodeCache[dirPath] = newDirNode;

            return newDirNode;
        }

        private TreeNode CreateFileNode(string filePath, string solutionPath, ScraperOptions options)
        {
            var fileInfo = new FileInfo(filePath);
            string nodeText = fileInfo.Name;

            if (options.ShowPerFileTokens)
            {
                nodeText += $" ({ProjectBuilder.GetTokenCountForFile(filePath, solutionPath, options)})";
            }

            return new TreeNode(nodeText)
            {
                Tag = PathHelper.GetSafeRelativePath(solutionPath, filePath)
            };
        }

        private bool IsSolutionItem(string filePath, ScraperOptions options)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return (options.IncludeJson && ext == ".json") ||
                   (options.IncludeConfig && ext == ".config") ||
                   (options.IncludeOtherFiles && !string.IsNullOrEmpty(ext));
        }

        public void PerformSearch(string term)
        {
            ResetNodeColors();
            if (string.IsNullOrEmpty(term)) return;

            var matches = new List<TreeNode>();
            FindNodesRecursive(_treeView.Nodes, term, matches);

            if (matches.Count > 0)
            {
                _logAction($"Found {matches.Count} matches for '{term}'.", Color.Cyan);
                foreach (var node in matches)
                {
                    node.BackColor = Color.Yellow;
                    node.ForeColor = Color.Black;
                    node.EnsureVisible();
                }
                _treeView.SelectedNode = matches[0];
                _treeView.Focus();
            }
            else
            {
                _logAction($"No matches found for '{term}'.", Color.Orange);
            }
        }

        public void ResetNodeColors()
        {
            void RecursiveReset(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    node.BackColor = _treeView.BackColor;
                    node.ForeColor = _treeView.ForeColor;
                    if (node.Nodes.Count > 0)
                    {
                        RecursiveReset(node.Nodes);
                    }
                }
            }
            RecursiveReset(_treeView.Nodes);
        }

        private void FindNodesRecursive(TreeNodeCollection nodes, string term, List<TreeNode> matches)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(node);
                }
                if (node.Nodes.Count > 0)
                {
                    FindNodesRecursive(node.Nodes, term, matches);
                }
            }
        }

        public void HandleAfterCheck(TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            CheckChildrenRecursive(e.Node, e.Node.Checked);

            UpdateParentNodeCheckState(e.Node.Parent);
        }

        private void CheckChildrenRecursive(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;

                if (child.Nodes.Count > 0)
                {
                    CheckChildrenRecursive(child, isChecked);
                }
            }
        }

        private void UpdateParentNodeCheckState(TreeNode? parent)
        {
            if (parent == null) return;

            bool newCheckState = parent.Nodes.Cast<TreeNode>().Any(n => n.Checked);

            if (parent.Checked != newCheckState)
            {
                parent.Checked = newCheckState;
            }

            UpdateParentNodeCheckState(parent.Parent);
        }

        public HashSet<string> GetCheckedFiles()
        {
            var checkedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void RecursiveGet(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Checked && node.Tag is string tagStr && Path.HasExtension(tagStr))
                    {
                        checkedFiles.Add(tagStr);
                    }
                    if (node.Nodes.Count > 0)
                    {
                        RecursiveGet(node.Nodes);
                    }
                }
            }
            RecursiveGet(_treeView.Nodes);
            return checkedFiles;
        }

        public List<string> GetAllFilePaths()
        {
            var allPaths = new List<string>();
            void RecursiveGet(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Tag is string tagStr && Path.HasExtension(tagStr))
                    {
                        allPaths.Add(tagStr);
                    }
                    if (node.Nodes.Count > 0)
                    {
                        RecursiveGet(node.Nodes);
                    }
                }
            }
            RecursiveGet(_treeView.Nodes);
            return allPaths;
        }

        public HashSet<string> GetCheckedTopLevelNodes()
        {
            return _treeView.Nodes.Cast<TreeNode>()
                .Where(node => node.Checked)
                .Select(node => node.Text)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public void RestoreExpansionState(HashSet<string>? expandedPaths)
        {
            if (expandedPaths == null) return;
            void RecursiveRestore(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {

                    if (node.Tag is string tagStr && expandedPaths.Contains(tagStr))
                    {
                        node.Expand();
                    }
                    if (node.Nodes.Count > 0)
                    {
                        RecursiveRestore(node.Nodes);
                    }
                }
            }
            RecursiveRestore(_treeView.Nodes);
        }

        public HashSet<string> GetExpansionState()
        {
            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void RecursiveSave(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.IsExpanded && node.Tag is string tagStr)
                    {

                        expandedPaths.Add(tagStr);
                    }
                    if (node.Nodes.Count > 0)
                    {
                        RecursiveSave(node.Nodes);
                    }
                }
            }
            RecursiveSave(_treeView.Nodes);
            return expandedPaths;
        }

        public string GetTreeAsText()
        {
            var sb = new StringBuilder();
            void BuildString(TreeNodeCollection nodes, string indent)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    bool isLast = (i == nodes.Count - 1);
                    string prefix = isLast ? "└── " : "├── ";
                    string checkState = node.Checked ? "[x] " : "[ ] ";
                    sb.AppendLine($"{indent}{prefix}{checkState}{node.Text}");
                    if (node.Nodes.Count > 0)
                    {
                        BuildString(node.Nodes, indent + (isLast ? "    " : "│   "));
                    }
                }
            }
            BuildString(_treeView.Nodes, "");
            return sb.ToString();
        }
    }
}