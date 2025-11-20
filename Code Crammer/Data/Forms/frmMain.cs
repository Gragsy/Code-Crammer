#nullable disable

using Newtonsoft.Json;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace Code_Crammer.Data.Forms_Classes
{
    public partial class frmMain : Form
    {
        #region " Class-Level Declarations & Nested Classes "

        public class ProfileData
        {
            public string SolutionPath { get; set; }
            public Dictionary<string, bool> OptionStates { get; set; }
            public HashSet<string> CheckedFiles { get; set; }
            public HashSet<string> ExpandedNodes { get; set; }

            public ProfileData()
            {
                OptionStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                CheckedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ExpandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public class SessionState
        {
            public HashSet<string> CheckedFiles { get; set; }
            public HashSet<string> ExpandedNodes { get; set; }

            public SessionState()
            {
                CheckedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ExpandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private const int TIMER_INTERVAL_REBUILD = 500;
        private const double TOKEN_CHARS_PER_TOKEN = 4.0;
        private const double TOKEN_OVERHEAD_MULTIPLIER = 1.08;
        private const long CLIPBOARD_SIZE_WARNING_BYTES = 50 * 1024 * 1024;
        private const long LARGE_FILE_THRESHOLD_BYTES = 1 * 1024 * 1024;
        private const int MAX_CACHE_SIZE = 1000;

        private readonly List<string> _excludeFolders = new List<string> { "\\bin\\", "\\obj\\", "\\.vs\\", "\\.git\\" };
        private readonly string _defaultSolutionPath = string.Empty;
        private readonly Random _random = new Random();
        private readonly List<string> _roasts = new List<string>();
        private readonly Code_Crammer.Data.LRUCache<string, string> _processedContentCache = new Code_Crammer.Data.LRUCache<string, string>(MAX_CACHE_SIZE);

        private List<string> _cachedProjectFiles = new List<string>();
        private string _baseFormTitle;
        private ScraperOptions _currentOptions;
        private string _currentProfilePath;
        private CancellationTokenSource _tokenCountCTS;
        private CancellationTokenSource _scraperCTS;

        private volatile int _tokenCountSequence = 0;

        private int _isCheckingNodeInt = 0;
        private bool _rebuildTreeRequired = false;
        private SemaphoreSlim _treeSemaphore = new SemaphoreSlim(1, 1);
        private bool _isRebuilding = false;
        private bool _treeStateBeingRestored = false;
        private bool _isClosing = false;
        private int _lastTooltipIndex = -1;
        private bool _isProgrammaticUpdate = false;
        private HashSet<string> _lastCheckedFiles;
        private HashSet<string> _lastExpandedNodes;
        private System.Windows.Forms.Timer tmrAutoSave = new System.Windows.Forms.Timer();

        #endregion " Class-Level Declarations & Nested Classes "

        #region " Enum Definitions "

        private enum ScraperOption
        {
            [Description("Code Files")] CodeFiles,
            [Description("Config Files")] ConfigFiles,
            [Description("Designer Files")] DesignerFiles,
            [Description("Include Other Files")] IncludeOtherFiles,
            [Description("Json Files")] JsonFiles,
            [Description("Project Files")] ProjectFiles,
            [Description("Resource Files")] ResourceFiles,
            [Description("Distill Project (Bible Mode)")] DistillProject,
            [Description("Distill Unused")] DistillUnused,
            [Description("Distill Active Projects Only")] DistillUnusedHeaders,
            [Description("Exclude Project Settings")] ExcludeMyProject,
            [Description("Remove Comments")] RemoveComments,
            [Description("Sanitize Files (Recommended)")] SanitizeFiles,
            [Description("Squish Designer Files")] SquishDesignerFiles,
            [Description("Copy To Clipboard")] CopyToClipboard,
            [Description("Create File")] CreateFile,
            [Description("Include Message")] IncludeMessage,
            [Description("Include Project Structure")] IncludeProjectStructure,
            [Description("Open File On Completion")] OpenFileOnCompletion,
            [Description("Open Folder On Completion")] OpenFolderOnCompletion,
            [Description("Show Per-File Token Counts")] ShowPerFileTokens
        }

        private string GetEnumDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return value.ToString();
            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        #endregion " Enum Definitions "

        public frmMain()
        {
            InitializeComponent();
            BindEvents();
        }

        private void BindEvents()
        {
            this.Load += frmMain_Load;
            this.Shown += frmMain_Shown;
            this.FormClosing += frmMain_FormClosing;

            btnSelectFolder.Click += btnSelectFolder_Click;
            btnGenerate.Click += btnGenerate_Click;
            btnDefault.Click += btnDefault_Click;
            btnEditMessage.Click += btnEditMessage_Click;
            btnLoad.Click += btnLoad_Click;
            btnSave.Click += btnSave_Click;
            btnSaveAs.Click += btnSaveAs_Click;

            ddlProfiles.SelectedIndexChanged += ddlProfiles_SelectedIndexChanged;

            txtSearch.KeyDown += txtSearch_KeyDown;
            txtSearch.TextChanged += txtSearch_TextChanged;

            tmrRepopulate.Tick += tmrRepopulate_Tick;
            tmrAutoSave.Tick += tmrAutoSave_Tick;

            tvwFiles.AfterCheck += tvwFiles_AfterCheck;
            tvwFiles.MouseDown += tvwFiles_MouseDown;
            tvwFiles.DragEnter += tvwFiles_DragEnter;
            tvwFiles.DragDrop += tvwFiles_DragDrop;
            this.DragEnter += tvwFiles_DragEnter;
            this.DragDrop += tvwFiles_DragDrop;

            clbFileTypes.ItemCheck += clbOptions_ItemCheck;
            clbProcessing.ItemCheck += clbOptions_ItemCheck;
            clbOutput.ItemCheck += clbOptions_ItemCheck;

            clbFileTypes.MouseMove += (s, e) => HandleListTooltip(clbFileTypes, e);
            clbProcessing.MouseMove += (s, e) => HandleListTooltip(clbProcessing, e);
            clbOutput.MouseMove += (s, e) => HandleListTooltip(clbOutput, e);

            ctmTreeView.Opening += ctmTreeView_Opening;
            mnuView.Click += mnuView_Click;
            mnuConvertToText.Click += mnuConvertToText_Click;
            mnuReset.Click += mnuReset_Click;
            mnuCollapseUnused.Click += mnuCollapseUnused_Click;
            mnuOpenFolder.Click += mnuOpenFolder_Click;
            mnuSelectAllParent.Click += mnuSelectAllParent_Click;
            mnuDeselectAllParent.Click += mnuDeselectAllParent_Click;
            mnuExpandAllParent.Click += mnuExpandAllParent_Click;
            mnuCollapseAllParent.Click += mnuCollapseAllParent_Click;
            mnuSelectAllGlobal.Click += mnuSelectAllGlobal_Click;
            mnuDeselectAllGlobal.Click += mnuDeselectAllGlobal_Click;
            mnuExpandAllGlobal.Click += mnuExpandAllGlobal_Click;
            mnuCollapseAllGlobal.Click += mnuCollapseAllGlobal_Click;

            mnuCopy.Click += mnuCopy_Click;
            mnuCopySelected.Click += mnuCopySelected_Click;
            mnuClear.Click += mnuClear_Click;
        }

        #region " Form Events "

        private void frmMain_Load(object sender, EventArgs e)
        {
            LoadRoastsFromFile();
            LanguageManager.Initialize();

            int roastIndex = _random.Next(0, _roasts.Count);
            _baseFormTitle = _roasts[roastIndex];
            UpdateFormTitle();

            LoadSettings();
            PopulateProfilesDropdown();

            tmrRepopulate.Interval = TIMER_INTERVAL_REBUILD;
            lblTokenCount.Text = "Approx. Tokens: 0";
            tmrAutoSave.Interval = 2000;

            TipTop.SetToolTip(btnSelectFolder, TooltipContent.GetTooltip("btnSelectFolder"));
            TipTop.SetToolTip(btnGenerate, TooltipContent.GetTooltip("btnGenerate"));
            TipTop.SetToolTip(btnDefault, TooltipContent.GetTooltip("btnDefault"));
            TipTop.SetToolTip(btnEditMessage, TooltipContent.GetTooltip("btnEditMessage"));
            TipTop.SetToolTip(txtFolderPath, TooltipContent.GetTooltip("txtFolderPath"));
            TipTop.SetToolTip(lblTokenCount, TooltipContent.GetTooltip("lblTokenCount"));
        }

        private async void frmMain_Shown(object sender, EventArgs e)
        {
            try
            {
                // LOGIC: Only scan if the text box is NOT empty and the directory actually exists.
                if (!string.IsNullOrEmpty(txtFolderPath.Text) && Directory.Exists(txtFolderPath.Text))
                {
                    btnGenerate.Enabled = true;
                    await PopulateFileTreeAsync();
                }
                else
                {
                    // If empty or invalid, do nothing. Wait for user to click "Select Folder".
                    btnGenerate.Enabled = false;
                    txtFolderPath.Text = string.Empty; // Clear invalid paths
                    btnSelectFolder.Focus();
                }
            }
            catch (Exception ex)
            {
                Log($"Error during startup population: {ex.Message}", Color.Red);
            }
        }

        private async void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing) return;

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _isClosing = true;
                this.Enabled = false;
                this.Text += " (Closing...)";
                this.FormClosing -= frmMain_FormClosing;

                try
                {
                    tmrAutoSave.Stop();
                    tmrAutoSave.Dispose();

                    _tokenCountCTS?.Cancel();
                    _scraperCTS?.Cancel();

                    var saveTask = SaveSettingsAsync();
                    var timeoutTask = Task.Delay(3000);

                    var completedTask = await Task.WhenAny(saveTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        Debug.WriteLine("Shutdown timed out while saving settings.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to save settings on close: {ex.Message}");
                }
                finally
                {
                    Application.Exit();
                }
            }
        }

        #endregion " Form Events "

        #region " UI Control Events "

        private async void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the root folder of your solution";
                dialog.UseDescriptionForTitle = true;

                if (!string.IsNullOrEmpty(txtFolderPath.Text) && Directory.Exists(txtFolderPath.Text))
                {
                    dialog.SelectedPath = txtFolderPath.Text;
                }
                else if (Directory.Exists(_defaultSolutionPath))
                {
                    dialog.SelectedPath = _defaultSolutionPath;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _processedContentCache.Clear();
                    txtFolderPath.Text = dialog.SelectedPath;
                    btnGenerate.Enabled = true;
                    Log("Solution folder selected: " + dialog.SelectedPath, Color.Cyan);

                    RequestSettingsSave();
                    try
                    {
                        await PopulateFileTreeAsync();
                    }
                    catch (Exception ex)
                    {
                        Log($"Error populating file tree: {ex.Message}", Color.Red);
                    }
                }
            }
        }

        private void clbOptions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_isRebuilding) return;

            SafeInvoke(() =>
            {
                SetMainControlsEnabled(false);
                RequestSettingsSave();
                _rebuildTreeRequired = true;
                tmrRepopulate.Stop();
                tmrRepopulate.Start();
            });
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            if (_roasts.Count <= 1) LoadRoastsFromFile();

            if (_scraperCTS != null && !_scraperCTS.IsCancellationRequested)
            {
                _scraperCTS.Cancel();
                btnGenerate.Enabled = false;
                btnGenerate.Text = "Cancelling...";
                return;
            }

            string solutionPath = txtFolderPath.Text;
            _scraperCTS = new CancellationTokenSource();
            Task<string> scraperTask = null;

            try
            {
                if (string.IsNullOrEmpty(solutionPath) || !Directory.Exists(solutionPath))
                {
                    Log("ERROR: Please select a valid solution folder first.", Color.Red);
                    return;
                }

                _currentOptions = new ScraperOptions
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
                    DistillUnusedHeaders = IsOptionChecked(ScraperOption.DistillUnusedHeaders)
                };

                var allFilesFromTree = GetAllFilePathsFromTree(tvwFiles.Nodes);
                var checkedProjects = GetCheckedTopLevelNodes(tvwFiles.Nodes);
                var selectedFiles = GetCheckedFiles(tvwFiles.Nodes);

                if (selectedFiles.Count == 0 && !_currentOptions.IncludeFolderLayout && !_currentOptions.DistillUnused && !_currentOptions.DistillUnusedHeaders)
                {
                    Log("ERROR: Please select at least one file or option.", Color.Red);
                    return;
                }

                string tokenText = lblTokenCount.Text;
                long estimatedTokens = 0;
                StringBuilder sbDigits = new StringBuilder();
                foreach (char c in tokenText)
                {
                    if (char.IsDigit(c)) sbDigits.Append(c);
                }

                if (long.TryParse(sbDigits.ToString(), out estimatedTokens))
                {
                    long estimatedChars = estimatedTokens * (long)TOKEN_CHARS_PER_TOKEN;
                    if (estimatedChars < 0 || estimatedChars > int.MaxValue)
                    {
                        MessageBox.Show(
                            $"Output is too large (>{int.MaxValue:N0} chars).\n\n" +
                            "Please deselect some files or enable 'Distill Project' mode.",
                            "Output Too Large",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    long estimatedBytes = estimatedChars;
                    if (_currentOptions.CopyToClipboard && estimatedBytes > CLIPBOARD_SIZE_WARNING_BYTES)
                    {
                        var warningResult = MessageBox.Show(
                            $"The estimated output size is very large (~{estimatedBytes / 1024.0 / 1024.0:N1} MB).\r\n" +
                            "Copying this to the clipboard may cause the application to hang or fail.\r\n\r\n" +
                            "Do you want to continue?",
                            "Large Output Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        if (warningResult == DialogResult.No) return;
                    }
                }

                string projectStructureString = string.Empty;
                if (_currentOptions.IncludeFolderLayout)
                {
                    StringBuilder sb = new StringBuilder();
                    ConvertTreeToString(tvwFiles.Nodes, sb, "");
                    projectStructureString = sb.ToString();
                }

                this.Cursor = Cursors.WaitCursor;
                btnGenerate.Text = "Cancel";
                rtbLog.Clear();
                Log("--- Starting Universal Project Scraping Operation ---", Color.Yellow);

                var progressHandler = new Progress<string>(progress =>
                {
                    if (!_isClosing) Log(progress);
                });

                scraperTask = Task.Run(() =>
                {
                    _scraperCTS.Token.ThrowIfCancellationRequested();
                    return GenerateProjectStateString(solutionPath, _currentOptions, allFilesFromTree, checkedProjects, selectedFiles, progressHandler, _scraperCTS.Token, projectStructureString);
                }, _scraperCTS.Token);

                string resultText = await scraperTask;

                if (_scraperCTS.IsCancellationRequested)
                {
                    Log("Operation Cancelled by user.", Color.Orange);
                    return;
                }

                await HandleScrapingSuccessAsync(resultText);
            }
            catch (OperationCanceledException)
            {
                Log("Operation Cancelled by user.", Color.Orange);
            }
            catch (Exception ex)
            {
                Log($"An unhandled error occurred: {ex.Message}", Color.Red);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnGenerate.Text = "Generate Project State File";
                btnGenerate.Enabled = true;
                _scraperCTS?.Dispose();
                _scraperCTS = null;
            }
        }

        private async void btnDefault_Click(object sender, EventArgs e)
        {
            string currentPath = txtFolderPath.Text;
            Properties.Settings.Default.Reset();
            LoadSettings();
            txtFolderPath.Text = currentPath;

            try
            {
                await PopulateFileTreeAsync();
                Log("Settings have been restored to their default values.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                Log($"Error restoring defaults: {ex.Message}", Color.Red);
            }
        }

        private void btnEditMessage_Click(object sender, EventArgs e)
        {
            using (var frm = new frmMessageEditor())
            {
                frm.ShowDialog();
            }
        }

        private async void tmrRepopulate_Tick(object sender, EventArgs e)
        {
            tmrRepopulate.Stop();
            if (_rebuildTreeRequired && !_isRebuilding)
            {
                _rebuildTreeRequired = false;
                try
                {
                    await PopulateFileTreeAsync();
                }
                catch (Exception ex)
                {
                    Log($"Error during timed repopulation: {ex.Message}", Color.Red);
                }
            }
        }

        #endregion " UI Control Events "

        #region " DragDrop, Search, Profiles "

        private void tvwFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void tvwFiles_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                string path = files[0];
                if (Directory.Exists(path))
                {
                    txtFolderPath.Text = path;
                    _processedContentCache.Clear();
                    Log($"Folder dropped: {path}", Color.Cyan);
                    RequestSettingsSave();
                    await PopulateFileTreeAsync();
                }
                else
                {
                    Log("Please drop a folder, not a file.", Color.Orange);
                }
            }
        }

        private void PopulateProfilesDropdown()
        {
            ddlProfiles.Items.Clear();
            ddlProfiles.Items.Add("Load Profile...");

            string profilesPath = PathManager.GetProfilesFolderPath();
            if (Directory.Exists(profilesPath))
            {
                var files = Directory.GetFiles(profilesPath, "*.json");
                foreach (var file in files)
                {
                    ddlProfiles.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }

        private void ddlProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate) return;
            if (ddlProfiles.SelectedIndex < 0) return;

            if (ddlProfiles.SelectedIndex == 0)
            {
                this.BeginInvoke(new Action(() => { ddlProfiles.SelectedIndex = -1; }));
                btnLoad.PerformClick();
                return;
            }

            string selectedProfile = ddlProfiles.SelectedItem.ToString();
            string fullPath = Path.Combine(PathManager.GetProfilesFolderPath(), selectedProfile + ".json");
            LoadProfile(fullPath);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                ResetNodeColors(tvwFiles.Nodes);
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string term = txtSearch.Text.Trim();
                if (!string.IsNullOrEmpty(term))
                {
                    SearchTree(term);
                }
                e.SuppressKeyPress = true;
            }
        }

        private void SearchTree(string term)
        {
            ResetNodeColors(tvwFiles.Nodes);
            var matches = new List<TreeNode>();
            FindNodesRecursive(tvwFiles.Nodes, term, matches);

            if (matches.Count > 0)
            {
                Log($"Found {matches.Count} matches for '{term}'.", Color.Cyan);
                foreach (var node in matches)
                {
                    node.BackColor = Color.Yellow;
                    node.ForeColor = Color.Black;
                    node.EnsureVisible();
                }
                tvwFiles.SelectedNode = matches[0];
                tvwFiles.Focus();
            }
            else
            {
                Log($"No matches found for '{term}'.", Color.Orange);
            }
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

        private void ResetNodeColors(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.BackColor = tvwFiles.BackColor;
                node.ForeColor = tvwFiles.ForeColor;
                if (node.Nodes.Count > 0) ResetNodeColors(node.Nodes);
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            using (var frm = new Code_Crammer.Data.Forms.frmAbout())
            {
                frm.ShowDialog(this);
            }
        }

        private void btnDonate_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://buymeacoffee.com/grags") { UseShellExecute = true });
            }
            catch { }
        }

        #endregion " DragDrop, Search, Profiles "

        #region " TreeView Management "

        private async Task PopulateFileTreeAsync(bool updateTokens = true)
        {
            if (!await _treeSemaphore.WaitAsync(0))
            {
                Log("Tree rebuild already in progress, skipping...", Color.Orange);
                return;
            }

            _isRebuilding = true;
            this.Cursor = Cursors.WaitCursor;

            tvwFiles.BeginUpdate();
            try
            {
                Log("Building file tree... (This may take a moment)", Color.Yellow);

                if (tvwFiles.Nodes.Count > 0)
                {
                    _lastCheckedFiles = GetCheckedFiles(tvwFiles.Nodes);
                    _lastExpandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    SaveExpansionState(tvwFiles.Nodes, _lastExpandedNodes);
                }

                tvwFiles.Nodes.Clear();
                string solutionPath = txtFolderPath.Text;

                if (string.IsNullOrEmpty(solutionPath) || !Directory.Exists(solutionPath))
                {
                    return;
                }

                var options = new ScraperOptions
                {
                    IncludeCode = IsOptionChecked(ScraperOption.CodeFiles),
                    IncludeProjectFile = IsOptionChecked(ScraperOption.ProjectFiles),
                    IncludeResx = IsOptionChecked(ScraperOption.ResourceFiles),
                    IncludeDesigner = IsOptionChecked(ScraperOption.DesignerFiles),
                    IncludeConfig = IsOptionChecked(ScraperOption.ConfigFiles),
                    IncludeJson = IsOptionChecked(ScraperOption.JsonFiles),
                    IncludeOtherFiles = IsOptionChecked(ScraperOption.IncludeOtherFiles),
                    ShowPerFileTokens = IsOptionChecked(ScraperOption.ShowPerFileTokens),
                    SanitizeOutput = IsOptionChecked(ScraperOption.SanitizeFiles),
                    RemoveComments = IsOptionChecked(ScraperOption.RemoveComments),
                    SquishDesignerFiles = IsOptionChecked(ScraperOption.SquishDesignerFiles)
                };

                bool excludeMyProject = IsOptionChecked(ScraperOption.ExcludeMyProject);
                _cachedProjectFiles.Clear();

                List<TreeNode> topLevelNodes = await Task.Run(() =>
                {
                    var nodes = new List<TreeNode>();
                    try
                    {
                        var projectFiles = SafeGetFiles(solutionPath);
                        _cachedProjectFiles.AddRange(projectFiles);

                        if (!projectFiles.Any())
                        {
                            Log("No .NET project files found. Scanning folder as a generic project.", Color.Cyan);
                            string rootName = new DirectoryInfo(solutionPath).Name;
                            var rootNode = new TreeNode(rootName);
                            nodes.Add(rootNode);

                            var projectFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            AddDirectoryNodes(rootNode, solutionPath, options, solutionPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase), ref projectFilePaths, excludeMyProject);
                        }
                        else
                        {
                            var projectFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            var solutionItemsNode = new TreeNode("--- SOLUTION ITEMS ---");
                            nodes.Add(solutionItemsNode);

                            foreach (var projFile in projectFiles.OrderBy(f => f))
                            {
                                string projName = Path.GetFileNameWithoutExtension(projFile);
                                string projDir = Path.GetDirectoryName(projFile);
                                var projectNode = new TreeNode(projName);
                                nodes.Add(projectNode);

                                AddDirectoryNodes(projectNode, projDir, options, solutionPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase), ref projectFilePaths, excludeMyProject);
                            }

                            var solutionFiles = Directory.GetFiles(solutionPath, "*.*", SearchOption.TopDirectoryOnly).Where(f =>
                            {
                                if (projectFilePaths.Contains(f)) return false;
                                string ext = Path.GetExtension(f).ToLowerInvariant();
                                return (options.IncludeJson && ext == ".json") ||
                                       (options.IncludeConfig && ext == ".config") ||
                                       (options.IncludeOtherFiles && !string.IsNullOrEmpty(ext));
                            });

                            foreach (var file in solutionFiles)
                            {
                                var fileNode = new TreeNode(Path.GetFileName(file));
                                fileNode.Tag = GetSafeRelativePath(solutionPath, file);
                                if (options.ShowPerFileTokens)
                                {
                                    fileNode.Text += $" ({GetTokenCountForFile(file, solutionPath, options)})";
                                }
                                solutionItemsNode.Nodes.Add(fileNode);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"FATAL ERROR during tree build: {ex.Message}", Color.Red);
                    }
                    return nodes;
                });

                for (int i = topLevelNodes.Count - 1; i >= 0; i--)
                {
                    if (topLevelNodes[i].Nodes.Count == 0)
                    {
                        topLevelNodes.RemoveAt(i);
                    }
                }

                tvwFiles.Nodes.AddRange(topLevelNodes.ToArray());

                _isCheckingNodeInt = 1;
                _treeStateBeingRestored = true;
                try
                {
                    RestoreTreeState(tvwFiles.Nodes, _lastCheckedFiles);
                    RestoreExpansionState(tvwFiles.Nodes, _lastExpandedNodes);
                }
                finally
                {
                    _isCheckingNodeInt = 0;
                    _treeStateBeingRestored = false;
                }

                int projectCount = topLevelNodes.Count(n => n.Text != "--- SOLUTION ITEMS ---");
                Log($"Found {projectCount} projects and built file tree.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                Log($"Error building file tree: {ex.Message}", Color.Red);
            }
            finally
            {
                tvwFiles.EndUpdate();
                this.Cursor = Cursors.Default;
                _isRebuilding = false;
                _treeSemaphore.Release();
                SetMainControlsEnabled(true);

                if (updateTokens)
                {
                    UpdateTokenCountAsync();
                }
            }
        }

        private void AddDirectoryNodes(TreeNode parentNode, string folderPath, ScraperOptions options, string solutionPath, HashSet<string> visited, ref HashSet<string> projectFiles, bool excludeMyProject)
        {
            if (parentNode == null || string.IsNullOrEmpty(folderPath)) return;

            string dirKey = Path.GetFullPath(folderPath);
            if (visited.Contains(dirKey)) return;
            visited.Add(dirKey);

            var excludedFileTypes = new List<string> {
                ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico",
                ".mp3", ".wav", ".mp4", ".avi", ".mov", ".wmv",
                ".zip", ".rar", ".7z", ".exe", ".dll", ".pdb", ".suo", ".user"
            };

            try
            {
                foreach (var directoryPath in Directory.GetDirectories(folderPath).OrderBy(d => d))
                {
                    string dirName = Path.GetFileName(directoryPath);

                    if (_excludeFolders.Any(f => string.Equals(dirName, f.Trim('\\'), StringComparison.OrdinalIgnoreCase))) continue;
                    if (excludeMyProject && (string.Equals(dirName, "My Project", StringComparison.OrdinalIgnoreCase) || string.Equals(dirName, "Properties", StringComparison.OrdinalIgnoreCase))) continue;

                    var dirNode = new TreeNode(dirName);
                    AddDirectoryNodes(dirNode, directoryPath, options, solutionPath, visited, ref projectFiles, excludeMyProject);

                    if (dirNode.Nodes.Count > 0)
                    {
                        parentNode.Nodes.Add(dirNode);
                    }
                }

                foreach (var filePath in Directory.GetFiles(folderPath).OrderBy(f => f))
                {
                    var fileInfo = new FileInfo(filePath);
                    string fileExt = fileInfo.Extension.ToLowerInvariant();

                    if (excludedFileTypes.Contains(fileExt)) continue;

                    bool addFile = false;
                    bool isCheckedByDefault = true;
                    var langProfile = Code_Crammer.Data.LanguageManager.GetProfileForExtension(fileExt);

                    if (langProfile != null)
                    {
                        if (fileExt == ".cs" || fileExt == ".vb")
                        {
                            bool isDesignerFile = fileInfo.Name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                                                  fileInfo.Name.EndsWith(".designer.vb", StringComparison.OrdinalIgnoreCase);

                            if (isDesignerFile && Code_Crammer.Data.FileProcessor.IsIgnoredDesigner(fileInfo.Name))
                            {
                                addFile = false;
                            }
                            else
                            {
                                addFile = isDesignerFile ? options.IncludeDesigner : options.IncludeCode;
                            }
                        }
                        else
                        {
                            addFile = options.IncludeCode;
                        }
                    }
                    else
                    {
                        switch (fileExt)
                        {
                            case ".csproj":
                            case ".vbproj":
                                addFile = options.IncludeProjectFile;
                                break;

                            case ".resx":
                                addFile = options.IncludeResx;
                                break;

                            case ".config":
                                addFile = options.IncludeConfig;
                                break;

                            case ".json":
                                addFile = options.IncludeJson;
                                break;

                            default:
                                if (options.IncludeOtherFiles)
                                {
                                    addFile = true;
                                    isCheckedByDefault = false;
                                }
                                break;
                        }
                    }

                    if (addFile)
                    {
                        var fileNode = new TreeNode(fileInfo.Name);
                        fileNode.Tag = GetSafeRelativePath(solutionPath, filePath);
                        fileNode.Checked = isCheckedByDefault;

                        if (options.ShowPerFileTokens)
                        {
                            fileNode.Text += $" ({GetTokenCountForFile(filePath, solutionPath, options)})";
                        }

                        parentNode.Nodes.Add(fileNode);
                        projectFiles.Add(filePath);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception ex)
            {
                Log($"Error processing directory {folderPath}: {ex.Message}", Color.Orange);
            }
        }

        private void tvwFiles_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown) return;
            if (_treeStateBeingRestored) return;

            if (Interlocked.CompareExchange(ref _isCheckingNodeInt, 1, 0) != 0) return;

            if (this.Disposing || this.IsDisposed) return;

            tvwFiles.BeginUpdate();
            try
            {
                void SetChildren(TreeNode node)
                {
                    if (node == null || node.Nodes == null) return;
                    foreach (TreeNode child in node.Nodes)
                    {
                        if (child.Checked != node.Checked)
                        {
                            child.Checked = node.Checked;
                        }
                        if (child.Nodes.Count > 0)
                        {
                            SetChildren(child);
                        }
                    }
                }

                SetChildren(e.Node);
                UpdateParentNodeCheckState(e.Node.Parent);
            }
            finally
            {
                Interlocked.Exchange(ref _isCheckingNodeInt, 0);
                tvwFiles.EndUpdate();
            }

            RequestSettingsSave();
            UpdateTokenCountAsync();
        }

        private void UpdateParentNodeCheckState(TreeNode startNode)
        {
            TreeNode currentNode = startNode;
            while (currentNode != null)
            {
                currentNode.Checked = currentNode.Nodes.Cast<TreeNode>().Any(n => n.Checked);
                currentNode = currentNode.Parent;
            }
        }

        private void tvwFiles_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode clickedNode = tvwFiles.GetNodeAt(e.X, e.Y);
                if (clickedNode != null)
                {
                    tvwFiles.SelectedNode = clickedNode;
                }
            }
        }

        #endregion " TreeView Management "

        #region " TreeView Context Menu "

        private void ctmTreeView_Opening(object sender, CancelEventArgs e)
        {
            if (tvwFiles.SelectedNode == null)
            {
                e.Cancel = true;
                return;
            }

            bool isFile = false;
            if (tvwFiles.SelectedNode.Tag != null && tvwFiles.SelectedNode.Tag is string tagStr)
            {
                isFile = !string.IsNullOrEmpty(tagStr) && Path.HasExtension(tagStr);
            }
            mnuView.Enabled = isFile;
        }

        private void mnuView_Click(object sender, EventArgs e)
        {
            if (tvwFiles.SelectedNode == null || !mnuView.Enabled) return;
            string relativePath = tvwFiles.SelectedNode.Tag?.ToString();

            if (string.IsNullOrEmpty(relativePath)) return;

            try
            {
                string solutionPath = txtFolderPath.Text;
                string normalizedBase = Path.GetFullPath(solutionPath);
                string normalizedFull = Path.GetFullPath(Path.Combine(solutionPath, relativePath));

                if (!normalizedFull.StartsWith(normalizedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                    !normalizedFull.Equals(normalizedBase, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"Security Warning: Attempted to open file outside solution: {normalizedFull}", Color.Red);
                    return;
                }

                if (File.Exists(normalizedFull))
                {
                    Process.Start("notepad.exe", normalizedFull);
                    Log($"Opening '{relativePath}' in Notepad.", Color.LimeGreen);
                }
                else
                {
                    Log($"Cannot open file: '{normalizedFull}' not found.", Color.Red);
                }
            }
            catch (Exception ex)
            {
                Log($"Error opening file: {ex.Message}", Color.Red);
            }
        }

        private void mnuConvertToText_Click(object sender, EventArgs e)
        {
            try
            {
                Log("Generating TreeView text file...", Color.Yellow);
                StringBuilder sb = new StringBuilder();
                ConvertTreeToString(tvwFiles.Nodes, sb, "");
                string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".txt");
                File.WriteAllText(tempFilePath, sb.ToString());
                Process.Start("notepad.exe", tempFilePath);
                Log("TreeView structure opened in Notepad.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                Log($"Error converting tree to text: {ex.Message}", Color.Red);
            }
        }

        private void mnuReset_Click(object sender, EventArgs e)
        {
            SetGlobalCheckState(true);
            Log("File selection has been reset.", Color.LimeGreen);
        }

        private void mnuCollapseUnused_Click(object sender, EventArgs e)
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
                Log("Collapsed all unused projects.", Color.LimeGreen);
            }
            finally
            {
                tvwFiles.EndUpdate();
            }
        }

        private void mnuOpenFolder_Click(object sender, EventArgs e)
        {
            if (tvwFiles.SelectedNode == null) return;
            string path = "";

            if (tvwFiles.SelectedNode.Tag != null)
            {
                string relativePath = tvwFiles.SelectedNode.Tag.ToString();
                string fullPath = Path.Combine(txtFolderPath.Text, relativePath);
                if (File.Exists(fullPath)) path = Path.GetDirectoryName(fullPath);
                else if (Directory.Exists(fullPath)) path = fullPath;
            }

            if (string.IsNullOrEmpty(path)) path = txtFolderPath.Text;

            try
            {
                if (Directory.Exists(path)) Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                Log($"Could not open folder: {ex.Message}", Color.Red);
            }
        }

        private void mnuSelectAllParent_Click(object sender, EventArgs e)
        {
            if (tvwFiles.SelectedNode == null) return;
            SetRecursiveCheckState(tvwFiles.SelectedNode, true);
        }

        private void mnuDeselectAllParent_Click(object sender, EventArgs e)
        {
            if (tvwFiles.SelectedNode == null) return;
            SetRecursiveCheckState(tvwFiles.SelectedNode, false);
        }

        private void mnuExpandAllParent_Click(object sender, EventArgs e)
        {
            if (tvwFiles.SelectedNode == null) return;
            tvwFiles.SelectedNode.ExpandAll();
        }

        private void mnuCollapseAllParent_Click(object sender, EventArgs e)
        {
            if (tvwFiles.SelectedNode == null) return;
            tvwFiles.SelectedNode.Collapse();
        }

        private void mnuSelectAllGlobal_Click(object sender, EventArgs e)
        {
            SetGlobalCheckState(true);
        }

        private void mnuDeselectAllGlobal_Click(object sender, EventArgs e)
        {
            SetGlobalCheckState(false);
        }

        private void mnuExpandAllGlobal_Click(object sender, EventArgs e)
        {
            tvwFiles.ExpandAll();
            Log("All folders have been expanded.", Color.LimeGreen);
        }

        private void mnuCollapseAllGlobal_Click(object sender, EventArgs e)
        {
            tvwFiles.CollapseAll();
            Log("All folders have been collapsed.", Color.LimeGreen);
        }

        #endregion " TreeView Context Menu "

        #region " TreeView State Persistence "

        private void RestoreTreeState(TreeNodeCollection nodes, HashSet<string> checkedFiles)
        {
            if (nodes == null || checkedFiles == null) return;

            var stack1 = new Stack<TreeNode>();
            var stack2 = new Stack<TreeNode>();

            foreach (TreeNode node in nodes)
            {
                stack1.Push(node);
            }

            while (stack1.Count > 0)
            {
                var node = stack1.Pop();
                stack2.Push(node);

                if (node.Tag != null && node.Tag is string tagStr)
                {
                    node.Checked = checkedFiles.Contains(tagStr);
                }

                foreach (TreeNode child in node.Nodes)
                {
                    stack1.Push(child);
                }
            }

            while (stack2.Count > 0)
            {
                var node = stack2.Pop();
                if (node.Nodes.Count > 0)
                {
                    node.Checked = node.Nodes.Cast<TreeNode>().Any(n => n.Checked);
                }
            }
        }

        private void SaveExpansionState(TreeNodeCollection nodes, HashSet<string> expandedPaths)
        {
            if (nodes == null) return;
            foreach (TreeNode node in nodes)
            {
                if (node.IsExpanded)
                {
                    expandedPaths.Add(node.FullPath);
                }
                if (node.Nodes.Count > 0)
                {
                    SaveExpansionState(node.Nodes, expandedPaths);
                }
            }
        }

        private void RestoreExpansionState(TreeNodeCollection nodes, HashSet<string> expandedPaths)
        {
            if (nodes == null) return;
            foreach (TreeNode node in nodes)
            {
                if (expandedPaths.Contains(node.FullPath))
                {
                    node.Expand();
                }
                if (node.Nodes.Count > 0)
                {
                    RestoreExpansionState(node.Nodes, expandedPaths);
                }
            }
        }

        #endregion " TreeView State Persistence "

        #region " Profile Management "

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = PathManager.GetProfilesFolderPath();
                dialog.Filter = "Profile Files (*.json)|*.json|All files (*.*)|*.*";
                dialog.Title = "Load Scraper Profile";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadProfile(dialog.FileName);
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProfilePath))
            {
                btnSaveAs_Click(sender, e);
            }
            else
            {
                SaveProfile(_currentProfilePath);
            }
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.InitialDirectory = PathManager.GetProfilesFolderPath();
                dialog.Filter = "Profile Files (*.json)|*.json|All files (*.*)|*.*";
                dialog.Title = "Save Scraper Profile As...";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SaveProfile(dialog.FileName);
                }
            }
        }

        private void LoadProfile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log($"Profile file not found: {filePath}", Color.Red);
                    return;
                }

                string jsonString = File.ReadAllText(filePath);
                var profile = JsonConvert.DeserializeObject<ProfileData>(jsonString);

                if (profile == null)
                {
                    Log("Failed to load profile: Invalid format", Color.Red);
                    return;
                }

                if (!string.IsNullOrEmpty(profile.SolutionPath) && Directory.Exists(profile.SolutionPath))
                {
                    txtFolderPath.Text = profile.SolutionPath;
                    btnGenerate.Enabled = true;
                }
                else
                {
                    Log($"Warning: Profile's solution path '{profile.SolutionPath}' could not be found. Using current path.", Color.Orange);
                }

                ApplyProfile(profile);
                _currentProfilePath = filePath;
                UpdateFormTitle();
                PopulateProfilesDropdown();

                string profileName = Path.GetFileNameWithoutExtension(filePath);
                if (ddlProfiles.Items.Contains(profileName))
                {
                    _isProgrammaticUpdate = true;
                    ddlProfiles.SelectedItem = profileName;
                    _isProgrammaticUpdate = false;
                }
                Log($"Profile '{profileName}' loaded successfully.", Color.LimeGreen);
            }
            catch (JsonException ex)
            {
                Log($"Error loading profile: Invalid JSON format - {ex.Message}", Color.Red);
            }
            catch (Exception ex)
            {
                Log($"Error loading profile: {ex.Message}", Color.Red);
            }
        }

        private void SaveProfile(string filePath)
        {
            try
            {
                var profile = GatherCurrentProfileData();
                string jsonString = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(filePath, jsonString);

                _currentProfilePath = filePath;
                UpdateFormTitle();
                PopulateProfilesDropdown();

                string profileName = Path.GetFileNameWithoutExtension(filePath);
                if (ddlProfiles.Items.Contains(profileName))
                {
                    _isProgrammaticUpdate = true;
                    ddlProfiles.SelectedItem = profileName;
                    _isProgrammaticUpdate = false;
                }
                Log($"Profile saved successfully to '{Path.GetFileName(filePath)}'.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                Log($"Error saving profile: {ex.Message}", Color.Red);
            }
        }

        private ProfileData GatherCurrentProfileData()
        {
            var profile = new ProfileData();
            profile.SolutionPath = txtFolderPath.Text;

            foreach (CheckedListBox box in new[] { clbFileTypes, clbProcessing, clbOutput })
            {
                for (int i = 0; i < box.Items.Count; i++)
                {
                    string itemName = box.Items[i].ToString();
                    profile.OptionStates[itemName] = box.GetItemChecked(i);
                }
            }

            profile.CheckedFiles = GetCheckedFiles(tvwFiles.Nodes);
            var expandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SaveExpansionState(tvwFiles.Nodes, expandedNodes);
            profile.ExpandedNodes = expandedNodes;

            return profile;
        }

        private async void ApplyProfile(ProfileData profile)
        {
            if (profile == null) return;

            clbFileTypes.ItemCheck -= clbOptions_ItemCheck;
            clbProcessing.ItemCheck -= clbOptions_ItemCheck;
            clbOutput.ItemCheck -= clbOptions_ItemCheck;

            try
            {
                foreach (CheckedListBox box in new[] { clbFileTypes, clbProcessing, clbOutput })
                {
                    for (int i = 0; i < box.Items.Count; i++)
                    {
                        string itemName = box.Items[i].ToString();
                        if (profile.OptionStates.ContainsKey(itemName))
                        {
                            box.SetItemChecked(i, profile.OptionStates[itemName]);
                        }
                    }
                }
            }
            finally
            {
                clbFileTypes.ItemCheck += clbOptions_ItemCheck;
                clbProcessing.ItemCheck += clbOptions_ItemCheck;
                clbOutput.ItemCheck += clbOptions_ItemCheck;
            }

            await PopulateFileTreeAsync(false);

            tvwFiles.BeginUpdate();
            _isCheckingNodeInt = 1;
            _treeStateBeingRestored = true;
            try
            {
                void CheckAction(TreeNodeCollection nodes)
                {
                    if (nodes == null) return;
                    foreach (TreeNode node in nodes)
                    {
                        bool isChecked = false;
                        if (node.Tag != null && node.Tag is string tagStr)
                        {
                            isChecked = profile.CheckedFiles.Contains(tagStr);
                        }
                        node.Checked = isChecked;
                        if (node.Nodes.Count > 0)
                        {
                            CheckAction(node.Nodes);
                            node.Checked = node.Nodes.Cast<TreeNode>().Any(n => n.Checked);
                        }
                    }
                }

                CheckAction(tvwFiles.Nodes);
                if (profile.ExpandedNodes != null)
                {
                    RestoreExpansionState(tvwFiles.Nodes, profile.ExpandedNodes);
                }
            }
            finally
            {
                _isCheckingNodeInt = 0;
                _treeStateBeingRestored = false;
                tvwFiles.EndUpdate();
            }

            UpdateTokenCountAsync();
        }

        private void UpdateFormTitle()
        {
            if (string.IsNullOrEmpty(_currentProfilePath))
            {
                this.Text = _baseFormTitle;
            }
            else
            {
                this.Text = $"{_baseFormTitle} - [{Path.GetFileNameWithoutExtension(_currentProfilePath)}]";
            }
        }

        #endregion " Profile Management "

        #region " Token Counting "

        private async void UpdateTokenCountAsync()
        {
            if (_treeSemaphore.CurrentCount == 0 || this.Disposing || this.IsDisposed) return;

            _tokenCountCTS?.Cancel();
            _tokenCountCTS = new CancellationTokenSource();

            var localCTS = _tokenCountCTS;
            int requestSequence = Interlocked.Increment(ref _tokenCountSequence);
            Task<long> tokenTask = null;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                lblTokenCount.Text = "Calculating...";

                string solutionPath = txtFolderPath.Text;
                var allFilePaths = GetAllFilePathsFromTree(tvwFiles.Nodes);
                var checkedProjects = GetCheckedTopLevelNodes(tvwFiles.Nodes);
                var selectedFiles = GetCheckedFiles(tvwFiles.Nodes);

                var tempOptions = new ScraperOptions
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
                    DistillUnusedHeaders = IsOptionChecked(ScraperOption.DistillUnusedHeaders)
                };

                string structureString = "";
                if (tempOptions.IncludeFolderLayout)
                {
                    StringBuilder sb = new StringBuilder();
                    ConvertTreeToString(tvwFiles.Nodes, sb, "");
                    structureString = sb.ToString();
                }

                var progressHandler = new Progress<string>(progress =>
                {
                    if (!_isClosing && !localCTS.IsCancellationRequested && requestSequence == _tokenCountSequence)
                    {
                        lblTokenCount.Text = progress;
                    }
                });

                tokenTask = Task.Run(() =>
                {
                    localCTS.Token.ThrowIfCancellationRequested();
                    try
                    {
                        ((IProgress<string>)progressHandler).Report("Calculating (Grouping files...)");
                        string finalOutput = GenerateProjectStateString(solutionPath, tempOptions, allFilePaths, checkedProjects, selectedFiles, progressHandler, localCTS.Token, structureString);
                        ((IProgress<string>)progressHandler).Report("Calculating (Finalizing...)");
                        return GetApproximateTokenCount(finalOutput);
                    }
                    catch
                    {
                        return 0L;
                    }
                }, localCTS.Token);

                long totalTokens = await tokenTask;

                if (requestSequence == _tokenCountSequence && !localCTS.Token.IsCancellationRequested && !this.IsDisposed)
                {
                    lblTokenCount.Text = $"Approx. Tokens: {totalTokens:N0}";
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!localCTS.Token.IsCancellationRequested && !this.IsDisposed)
                {
                    lblTokenCount.Text = "Error Counting";
                    Log($"Error during token count: {ex.Message}", Color.Red);
                }
            }
            finally
            {
                if (requestSequence == _tokenCountSequence && !localCTS.Token.IsCancellationRequested && !this.IsDisposed)
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private long GetApproximateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            double baseCount = text.Length / TOKEN_CHARS_PER_TOKEN;
            double finalCount = baseCount * TOKEN_OVERHEAD_MULTIPLIER;
            return (long)Math.Ceiling(finalCount);
        }

        #endregion " Token Counting "

        #region " File Content Processing "

        private string GenerateProjectStateString(string solutionPath, ScraperOptions options, List<string> allFiles, HashSet<string> checkedProjects, HashSet<string> selectedFiles, IProgress<string> progress, CancellationToken ct, string projectStructure)
        {
            try
            {
                StringBuilder report = new StringBuilder();
                report.AppendLine($"CODE CRAMMER - {Path.GetFileName(solutionPath)}");
                report.AppendLine($"Generated on: {DateTime.Now}");
                report.AppendLine();

                if (options.IncludeFolderLayout && !string.IsNullOrEmpty(projectStructure))
                {
                    report.AppendLine("--- PROJECT STRUCTURE ---");
                    report.AppendLine("```");
                    report.AppendLine(projectStructure);
                    report.AppendLine("```");
                    report.AppendLine();
                }

                progress?.Report("Grouping files by project...");
                var projectGroups = GroupFilesByProject(solutionPath, allFiles, checkedProjects, selectedFiles, options);

                ct.ThrowIfCancellationRequested();

                string fileReportPart = BuildReportFromGroupedFiles(solutionPath, projectGroups, selectedFiles, options, progress, ct);
                report.Append(fileReportPart);

                ct.ThrowIfCancellationRequested();

                if (options.IncludeMessage)
                {
                    string messageContent = LoadMessageContent();
                    if (!string.IsNullOrWhiteSpace(messageContent))
                    {
                        report.AppendLine(messageContent);
                    }
                }

                return report.ToString();
            }
            catch (OperationCanceledException)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                progress?.Report($"FATAL ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    progress?.Report($"INNER EXCEPTION: {ex.InnerException.Message}");
                }
                progress?.Report($"STACK TRACE: {ex.StackTrace}");
                return string.Empty;
            }
        }

        private Dictionary<string, List<string>> GroupFilesByProject(string solutionPath, List<string> allFiles, HashSet<string> checkedProjects, HashSet<string> selectedFiles, ScraperOptions options)
        {
            var projectGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            projectGroups.Add(string.Empty, new List<string>());

            var allProjectPaths = _cachedProjectFiles;
            if (allProjectPaths.Count == 0)
            {
                string rootName = new DirectoryInfo(solutionPath).Name;
                displayNames[solutionPath] = rootName;
                if (!projectGroups.ContainsKey(solutionPath))
                {
                    projectGroups.Add(solutionPath, new List<string>());
                }
            }

            foreach (var projPath in allProjectPaths)
            {
                string projDir = Path.GetDirectoryName(projPath);
                if (!projectGroups.ContainsKey(projDir))
                {
                    projectGroups.Add(projDir, new List<string>());
                    displayNames.Add(projDir, Path.GetFileNameWithoutExtension(projPath));
                }
            }

            bool isAnyDistillModeActive = options.DistillUnused || options.DistillUnusedHeaders;
            List<string> pathsToProcess;

            if (isAnyDistillModeActive)
            {
                if (options.DistillUnusedHeaders)
                {
                    var projectDirsForHeaders = displayNames.Where(kvp => checkedProjects.Contains(kvp.Value)).Select(kvp => kvp.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    bool isSolutionItemsChecked = checkedProjects.Contains("--- SOLUTION ITEMS ---");

                    pathsToProcess = allFiles.Where(relPath =>
                    {
                        string fullPath = Path.Combine(solutionPath, relPath);
                        string parentProjDir = projectDirsForHeaders.FirstOrDefault(dir => fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
                        return parentProjDir != null || (isSolutionItemsChecked && Path.GetDirectoryName(fullPath).Equals(solutionPath, StringComparison.OrdinalIgnoreCase));
                    }).ToList();
                }
                else
                {
                    pathsToProcess = allFiles;
                }
            }
            else
            {
                pathsToProcess = selectedFiles.ToList();
            }

            foreach (var relativePath in pathsToProcess)
            {
                string fullPath = Path.Combine(solutionPath, relativePath);
                string parentProjectDir = projectGroups.Keys
                    .Where(dir => !string.IsNullOrEmpty(dir) && fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(dir => dir.Length)
                    .FirstOrDefault();

                if (parentProjectDir != null)
                {
                    projectGroups[parentProjectDir].Add(fullPath);
                }
                else
                {
                    projectGroups[string.Empty].Add(fullPath);
                }
            }

            return projectGroups;
        }

        private string BuildReportFromGroupedFiles(string solutionPath, Dictionary<string, List<string>> projectGroups, HashSet<string> selectedFiles, ScraperOptions options, IProgress<string> progress, CancellationToken ct)
        {
            StringBuilder report = new StringBuilder();
            var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (_cachedProjectFiles.Count == 0)
            {
                string rootName = new DirectoryInfo(solutionPath).Name;
                displayNames.TryAdd(solutionPath, rootName);
            }
            else
            {
                foreach (var projPath in _cachedProjectFiles)
                {
                    displayNames.TryAdd(Path.GetDirectoryName(projPath), Path.GetFileNameWithoutExtension(projPath));
                }
            }

            foreach (var groupKey in projectGroups.Keys.OrderBy(k => string.IsNullOrEmpty(k) ? " " : displayNames.GetValueOrDefault(k, k)))
            {
                ct.ThrowIfCancellationRequested();

                string header = string.IsNullOrEmpty(groupKey) ? "--- SOLUTION ITEMS ---" : $"--- PROJECT: {displayNames.GetValueOrDefault(groupKey, "Unknown")}";
                var filesInGroup = projectGroups[groupKey].Distinct().OrderBy(f => f).ToList();

                if (!filesInGroup.Any()) continue;

                report.AppendLine(header);
                report.AppendLine();

                foreach (var filePath in filesInGroup)
                {
                    ct.ThrowIfCancellationRequested();
                    string currentRelativePath = GetSafeRelativePath(solutionPath, filePath);

                    try
                    {
                        progress?.Report($"...adding {currentRelativePath}");
                        bool isSelected = selectedFiles.Contains(currentRelativePath);
                        bool forceDistill = (options.DistillUnused || options.DistillUnusedHeaders) && !isSelected;

                        string fileContent = GetCachedProcessedFileContent(filePath, solutionPath, options, forceDistill);

                        if (string.IsNullOrWhiteSpace(fileContent))
                        {
                            progress?.Report($"    ...skipping empty or sanitized file: {currentRelativePath}");
                            continue;
                        }

                        report.AppendLine($"--- FILE: {currentRelativePath} ---");
                        string ext = Path.GetExtension(filePath).ToLowerInvariant();
                        string mdTag = "";

                        if (ext == ".cs") mdTag = "csharp";
                        else if (ext == ".vb") mdTag = "vb";
                        else if (ext == ".json") mdTag = "json";
                        else if (ext == ".xml" || ext == ".csproj" || ext == ".vbproj" || ext == ".config") mdTag = "xml";

                        report.AppendLine($"```{mdTag}");
                        report.AppendLine(fileContent);
                        report.AppendLine("```");
                        report.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"    ...error reading file {currentRelativePath}: {ex.Message}");
                    }
                }
            }
            return report.ToString();
        }

        #endregion " File Content Processing "

        #region " Settings Management "

        private void LoadSettings()
        {
            string lastPath = Properties.Settings.Default.LastFolderPath;

            // FIX: Explicitly check if the saved path is "C:\" or "C:" and ignore it.
            // This clears the "Bad Default" from previous versions.
            if (string.Equals(lastPath, @"C:\", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lastPath, @"C:", StringComparison.OrdinalIgnoreCase))
            {
                lastPath = string.Empty;
            }

            // Standard Logic: If it exists (and isn't the banned C:\), load it.
            if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            {
                txtFolderPath.Text = lastPath;
            }
            else
            {
                txtFolderPath.Text = string.Empty;
            }

            // Load Session State
            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastSessionStateJson))
            {
                try
                {
                    var state = JsonConvert.DeserializeObject<SessionState>(Properties.Settings.Default.LastSessionStateJson);
                    _lastCheckedFiles = state?.CheckedFiles;
                    _lastExpandedNodes = state?.ExpandedNodes ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                catch
                {
                    _lastCheckedFiles = null;
                    _lastExpandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            else
            {
                _lastCheckedFiles = null;
                _lastExpandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            clbFileTypes.ItemCheck -= clbOptions_ItemCheck;
            clbProcessing.ItemCheck -= clbOptions_ItemCheck;
            clbOutput.ItemCheck -= clbOptions_ItemCheck;

            try
            {
                SetOptionChecked(ScraperOption.IncludeProjectStructure, Properties.Settings.Default.IncludeProjectStructure);
                SetOptionChecked(ScraperOption.CodeFiles, Properties.Settings.Default.IncludeCodeFiles);
                SetOptionChecked(ScraperOption.ConfigFiles, Properties.Settings.Default.IncludeConfigFiles);
                SetOptionChecked(ScraperOption.DesignerFiles, Properties.Settings.Default.IncludeDesignerFiles);
                SetOptionChecked(ScraperOption.SquishDesignerFiles, Properties.Settings.Default.SquishDesignerFiles);
                SetOptionChecked(ScraperOption.JsonFiles, Properties.Settings.Default.IncludeJsonFiles);
                SetOptionChecked(ScraperOption.ProjectFiles, Properties.Settings.Default.IncludeProjectFiles);
                SetOptionChecked(ScraperOption.ResourceFiles, Properties.Settings.Default.IncludeResourceFiles);
                SetOptionChecked(ScraperOption.SanitizeFiles, Properties.Settings.Default.SanitizeFiles);
                SetOptionChecked(ScraperOption.RemoveComments, Properties.Settings.Default.RemoveComments);
                SetOptionChecked(ScraperOption.CreateFile, Properties.Settings.Default.CreateFile);
                SetOptionChecked(ScraperOption.OpenFileOnCompletion, Properties.Settings.Default.OpenFileAfterFinish);
                SetOptionChecked(ScraperOption.OpenFolderOnCompletion, Properties.Settings.Default.OpenFolderOnFinish);
                SetOptionChecked(ScraperOption.CopyToClipboard, Properties.Settings.Default.CopyToClipboard);
                SetOptionChecked(ScraperOption.IncludeMessage, Properties.Settings.Default.IncludeFancyMessage);
                SetOptionChecked(ScraperOption.ExcludeMyProject, Properties.Settings.Default.ExcludeMyProject);
                SetOptionChecked(ScraperOption.ShowPerFileTokens, Properties.Settings.Default.ShowPerFileTokens);
                SetOptionChecked(ScraperOption.DistillProject, Properties.Settings.Default.DistillProject);
                SetOptionChecked(ScraperOption.DistillUnused, Properties.Settings.Default.DistillUnused);
                SetOptionChecked(ScraperOption.DistillUnusedHeaders, Properties.Settings.Default.DistillUnusedHeaders);
            }
            finally
            {
                clbFileTypes.ItemCheck += clbOptions_ItemCheck;
                clbProcessing.ItemCheck += clbOptions_ItemCheck;
                clbOutput.ItemCheck += clbOptions_ItemCheck;
            }
        }

        private async Task SaveSettingsAsync()
        {
            Properties.Settings.Default.LastFolderPath = txtFolderPath.Text;
            Properties.Settings.Default.IncludeProjectStructure = IsOptionChecked(ScraperOption.IncludeProjectStructure);
            Properties.Settings.Default.IncludeCodeFiles = IsOptionChecked(ScraperOption.CodeFiles);
            Properties.Settings.Default.IncludeConfigFiles = IsOptionChecked(ScraperOption.ConfigFiles);
            Properties.Settings.Default.IncludeDesignerFiles = IsOptionChecked(ScraperOption.DesignerFiles);
            Properties.Settings.Default.SquishDesignerFiles = IsOptionChecked(ScraperOption.SquishDesignerFiles);
            Properties.Settings.Default.IncludeJsonFiles = IsOptionChecked(ScraperOption.JsonFiles);
            Properties.Settings.Default.IncludeProjectFiles = IsOptionChecked(ScraperOption.ProjectFiles);
            Properties.Settings.Default.IncludeResourceFiles = IsOptionChecked(ScraperOption.ResourceFiles);
            Properties.Settings.Default.SanitizeFiles = IsOptionChecked(ScraperOption.SanitizeFiles);
            Properties.Settings.Default.RemoveComments = IsOptionChecked(ScraperOption.RemoveComments);
            Properties.Settings.Default.CreateFile = IsOptionChecked(ScraperOption.CreateFile);
            Properties.Settings.Default.OpenFolderOnFinish = IsOptionChecked(ScraperOption.OpenFolderOnCompletion);
            Properties.Settings.Default.OpenFileAfterFinish = IsOptionChecked(ScraperOption.OpenFileOnCompletion);
            Properties.Settings.Default.CopyToClipboard = IsOptionChecked(ScraperOption.CopyToClipboard);
            Properties.Settings.Default.IncludeFancyMessage = IsOptionChecked(ScraperOption.IncludeMessage);
            Properties.Settings.Default.ExcludeMyProject = IsOptionChecked(ScraperOption.ExcludeMyProject);
            Properties.Settings.Default.ShowPerFileTokens = IsOptionChecked(ScraperOption.ShowPerFileTokens);
            Properties.Settings.Default.DistillProject = IsOptionChecked(ScraperOption.DistillProject);
            Properties.Settings.Default.DistillUnused = IsOptionChecked(ScraperOption.DistillUnused);
            Properties.Settings.Default.DistillUnusedHeaders = IsOptionChecked(ScraperOption.DistillUnusedHeaders);

            var sessionState = new SessionState();
            sessionState.CheckedFiles = GetCheckedFiles(tvwFiles.Nodes);
            sessionState.ExpandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SaveExpansionState(tvwFiles.Nodes, sessionState.ExpandedNodes);

            await Task.Run(() =>
            {
                Properties.Settings.Default.LastSessionStateJson = JsonConvert.SerializeObject(sessionState);
                Properties.Settings.Default.Save();
            });
        }

        private void RequestSettingsSave()
        {
            if (_isClosing) return;
            tmrAutoSave.Stop();
            tmrAutoSave.Start();
        }

        private async void tmrAutoSave_Tick(object sender, EventArgs e)
        {
            tmrAutoSave.Stop();
            try
            {
                await SaveSettingsAsync();
                Log("Session state auto-saved.", Color.Gray);
            }
            catch { }
        }

        #endregion " Settings Management "

        #region " Log Context Menu "

        private void mnuCopy_Click(object sender, EventArgs e)
        {
            if (rtbLog.TextLength > 0)
            {
                Clipboard.SetText(rtbLog.Text);
            }
        }

        private void mnuCopySelected_Click(object sender, EventArgs e)
        {
            if (rtbLog.SelectionLength > 0)
            {
                Clipboard.SetText(rtbLog.SelectedText);
            }
        }

        private void mnuClear_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        #endregion " Log Context Menu "

        #region " Tooltip Logic "

        private void HandleListTooltip(CheckedListBox list, MouseEventArgs e)
        {
            int index = list.IndexFromPoint(e.Location);
            if (index != _lastTooltipIndex)
            {
                _lastTooltipIndex = index;
                if (index >= 0)
                {
                    string itemText = list.Items[index].ToString();
                    string tip = TooltipContent.GetTooltip(itemText);
                    TipTop.SetToolTip(list, tip);
                }
                else
                {
                    TipTop.SetToolTip(list, null);
                }
            }
        }

        #endregion " Tooltip Logic "

        #region " Helper Functions "

        private void SetMainControlsEnabled(bool enabled)
        {
            SetOptionBoxesEnabled(enabled);
            btnSelectFolder.Enabled = enabled;
            btnGenerate.Enabled = enabled;
            btnDefault.Enabled = enabled;
            btnEditMessage.Enabled = enabled;
            tsMenu.Enabled = enabled;
        }

        private long GetTokenCountForFile(string filePath, string solutionPath, ScraperOptions options)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > LARGE_FILE_THRESHOLD_BYTES)
                {
                    return (long)Math.Ceiling((fileInfo.Length / TOKEN_CHARS_PER_TOKEN) * TOKEN_OVERHEAD_MULTIPLIER);
                }

                string processedContent = GetCachedProcessedFileContent(filePath, solutionPath, options, false);
                return GetApproximateTokenCount(processedContent);
            }
            catch (Exception ex)
            {
                Log($"Error counting tokens for {Path.GetFileName(filePath)}: {ex.Message}", Color.Orange);
                return 0;
            }
        }

        private string GetCachedProcessedFileContent(string filePath, string rootPath, ScraperOptions options, bool forceDistill)
        {
            try
            {
                string fullPath = Path.GetFullPath(filePath);
                string fullRoot = Path.GetFullPath(rootPath);
                if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"SECURITY BLOCK: Attempted to access file outside solution: {filePath}", Color.Red);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log($"Path Error: {ex.Message}", Color.Red);
                return string.Empty;
            }

            string cacheKey = $"{filePath}|{options.SanitizeOutput}|{options.RemoveComments}|{options.SquishDesignerFiles}|{options.DistillProject}|{options.DistillUnusedHeaders}|{options.DistillUnused}|{forceDistill}";

            if (_processedContentCache.TryGetValue(cacheKey, out string cachedValue))
            {
                return cachedValue;
            }

            if (!File.Exists(filePath))
            {
                Log($"File not found (skipped): {filePath}", Color.Orange);
                return string.Empty;
            }

            string fileContent = string.Empty;
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    fileContent = sr.ReadToEnd();
                }
            }
            catch (IOException)
            {
                Log($"Skipped locked file: {Path.GetFileName(filePath)}", Color.Orange);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log($"Error reading {Path.GetFileName(filePath)}: {ex.Message}", Color.Red);
                return string.Empty;
            }

            string processedContent = Code_Crammer.Data.FileProcessor.ProcessFile(filePath, fileContent, options, forceDistill);
            _processedContentCache.Add(cacheKey, processedContent);
            return processedContent;
        }

        private HashSet<string> GetCheckedFiles(TreeNodeCollection nodes)
        {
            var checkedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void RecursiveGet(TreeNodeCollection currentNodes)
            {
                if (currentNodes == null) return;
                foreach (TreeNode node in currentNodes)
                {
                    if (node.Checked)
                    {
                        if (node.Tag != null && node.Tag is string tagStr)
                        {
                            if (!string.IsNullOrEmpty(tagStr) && Path.HasExtension(tagStr))
                            {
                                checkedFiles.Add(tagStr);
                            }
                        }
                        if (node.Nodes.Count > 0) RecursiveGet(node.Nodes);
                    }
                }
            }

            RecursiveGet(nodes);
            return checkedFiles;
        }

        private List<string> GetAllFilePathsFromTree(TreeNodeCollection nodes)
        {
            var allPaths = new List<string>();
            if (nodes == null || nodes.Count == 0) return allPaths;

            var nodeQueue = new Queue<TreeNode>();
            foreach (TreeNode node in nodes)
            {
                nodeQueue.Enqueue(node);
            }

            while (nodeQueue.Count > 0)
            {
                var currentNode = nodeQueue.Dequeue();

                if (currentNode.Tag != null && currentNode.Tag is string tagStr)
                {
                    if (!string.IsNullOrEmpty(tagStr) && Path.HasExtension(tagStr))
                    {
                        allPaths.Add(tagStr);
                    }
                }

                foreach (TreeNode childNode in currentNode.Nodes)
                {
                    nodeQueue.Enqueue(childNode);
                }
            }
            return allPaths;
        }

        private HashSet<string> GetCheckedTopLevelNodes(TreeNodeCollection nodes)
        {
            var checkedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (nodes == null) return checkedProjects;

            foreach (TreeNode node in nodes)
            {
                if (node.Parent == null && node.Checked)
                {
                    checkedProjects.Add(node.Text);
                }
            }
            return checkedProjects;
        }

        private void SetOptionChecked(ScraperOption optionValue, bool isChecked)
        {
            string optionText = GetEnumDescription(optionValue);
            foreach (CheckedListBox box in new[] { clbFileTypes, clbProcessing, clbOutput })
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
            foreach (CheckedListBox box in new[] { clbFileTypes, clbProcessing, clbOutput })
            {
                int itemIndex = box.Items.IndexOf(optionText);
                if (itemIndex != -1)
                {
                    return box.GetItemChecked(itemIndex);
                }
            }
            Log($"Could not find option '{optionText}' in the lists.", Color.Red);
            return false;
        }

        private void SetOptionBoxesEnabled(bool enabled)
        {
            foreach (CheckedListBox box in new[] { clbFileTypes, clbProcessing, clbOutput })
            {
                box.Enabled = enabled;
            }
        }

        private string LoadMessageContent()
        {
            try
            {
                string messageFilePath = Path.Combine(PathManager.GetDataFolderPath(), "msg.txt");
                return File.Exists(messageFilePath) ? File.ReadAllText(messageFilePath, Encoding.UTF8) : string.Empty;
            }
            catch (Exception ex)
            {
                Log($"Could not load message file: {ex.Message}", Color.Orange);
                return string.Empty;
            }
        }

        private void Log(string message, Color color)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string, Color>(Log), message, color);
            }
            else
            {
                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.SelectionColor = color;
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                rtbLog.ScrollToCaret();
            }
        }

        private void Log(string message)
        {
            Log(message, Color.White);
        }

        private void ConvertTreeToString(TreeNodeCollection rootNodes, StringBuilder sb, string unusedIndent)
        {
            if (rootNodes == null || rootNodes.Count == 0) return;

            var stack = new Stack<(TreeNode Node, string Indent, bool IsLast)>();

            for (int i = rootNodes.Count - 1; i >= 0; i--)
            {
                stack.Push((rootNodes[i], "", i == rootNodes.Count - 1));
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.Node == null) continue;

                TreeNode node = current.Node;
                string indent = current.Indent;
                bool isLast = current.IsLast;

                string prefix = isLast ? "└── " : "├── ";
                string checkState = node.Checked ? "[x] " : "[ ] ";

                sb.AppendLine($"{indent}{prefix}{checkState}{node.Text}");

                if (node.Nodes.Count > 0)
                {
                    string newIndent = indent + (isLast ? "    " : "│   ");
                    for (int i = node.Nodes.Count - 1; i >= 0; i--)
                    {
                        stack.Push((node.Nodes[i], newIndent, i == node.Nodes.Count - 1));
                    }
                }
            }
        }

        private void SetRecursiveCheckState(TreeNode node, bool isChecked)
        {
            tvwFiles.BeginUpdate();
            System.Threading.Interlocked.Exchange(ref _isCheckingNodeInt, 1);
            try
            {
                void CheckRecursive(TreeNode target)
                {
                    target.Checked = isChecked;
                    foreach (TreeNode child in target.Nodes)
                    {
                        CheckRecursive(child);
                    }
                }
                CheckRecursive(node);
                UpdateParentNodeCheckState(node.Parent);
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _isCheckingNodeInt, 0);
                tvwFiles.EndUpdate();
            }
            UpdateTokenCountAsync();
        }

        private void SetGlobalCheckState(bool isChecked)
        {
            tvwFiles.BeginUpdate();
            System.Threading.Interlocked.Exchange(ref _isCheckingNodeInt, 1);
            try
            {
                void CheckRecursive(TreeNodeCollection nodes)
                {
                    if (nodes == null) return;
                    foreach (TreeNode node in nodes)
                    {
                        node.Checked = isChecked;
                        if (node.Nodes.Count > 0) CheckRecursive(node.Nodes);
                    }
                }
                CheckRecursive(tvwFiles.Nodes);
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _isCheckingNodeInt, 0);
                tvwFiles.EndUpdate();
            }
            UpdateTokenCountAsync();
            string action = isChecked ? "Selected" : "Deselected";
            Log($"{action} all files globally.", Color.LimeGreen);
        }

        private async Task HandleScrapingSuccessAsync(string resultText)
        {
            long finalTokens = GetApproximateTokenCount(resultText);
            lblTokenCount.Text = $"Final Count: {finalTokens:N0}";
            Log($"Final token count: {finalTokens:N0}", Color.LimeGreen);

            var successActions = new List<string>();
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string outputFilePath = "";

            Log("--- OPERATION COMPLETE ---", Color.Yellow);

            if (_currentOptions.CreateFile)
            {
                try
                {
                    if (!Directory.Exists(downloadsPath))
                    {
                        downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    }
                    outputFilePath = Path.Combine(downloadsPath, "_ProjectState.txt");
                    File.WriteAllText(outputFilePath, resultText);
                    Log($"Success! Project state file created at: {outputFilePath}", Color.LimeGreen);
                    successActions.Add($"_ProjectState.txt has been saved to your Downloads folder.");
                }
                catch (Exception ex)
                {
                    Log($"Error creating file: {ex.Message}", Color.Red);
                }
            }

            if (_currentOptions.CopyToClipboard)
            {
                bool clipboardSuccess = false;
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        long byteCount = Encoding.UTF8.GetByteCount(resultText);
                        if (byteCount > CLIPBOARD_SIZE_WARNING_BYTES)
                        {
                            Log($"WARNING: Output size is very large ({byteCount / 1024.0 / 1024.0:N2} MB). Copying to clipboard may fail or cause instability.", Color.Orange);
                        }

                        Clipboard.SetText(resultText);
                        clipboardSuccess = true;
                        break;
                    }
                    catch (System.Runtime.InteropServices.ExternalException)
                    {
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Log($"Could not copy to clipboard: {ex.Message}", Color.Orange);
                        break;
                    }
                }

                if (clipboardSuccess)
                {
                    Log("Success! Project state has been copied to the clipboard.", Color.LimeGreen);
                    successActions.Add("Project state has been copied to the clipboard.");
                }
                else if (!clipboardSuccess)
                {
                    Log("Failed to access Clipboard after multiple attempts.", Color.Red);
                }
            }

            if (_currentOptions.OpenFolderOnCompletion)
            {
                try
                {
                    Process.Start("explorer.exe", downloadsPath);
                    successActions.Add("Downloads folder has been opened.");
                }
                catch (Exception ex)
                {
                    Log($"Could not open output folder: {ex.Message}", Color.Orange);
                }
            }

            if (_currentOptions.OpenFileOnCompletion && _currentOptions.CreateFile && !string.IsNullOrEmpty(outputFilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(outputFilePath) { UseShellExecute = true });
                    successActions.Add("The project state file has been opened.");
                }
                catch (Exception ex)
                {
                    Log($"Could not open output file: {ex.Message}", Color.Orange);
                }
            }
            else if (_currentOptions.OpenFileOnCompletion && !_currentOptions.CreateFile)
            {
                Log("Skipping 'Open File On Completion' because 'Create File' was not selected.", Color.Orange);
            }

            if (successActions.Any())
            {
                MessageBox.Show("Success!\r\n\r\n" + string.Join("\r\n", successActions),
                    "Operation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void LoadRoastsFromFile()
        {
            _roasts.Clear();
            string roastsFilePath = Path.Combine(PathManager.GetDataFolderPath(), "Roasts.txt");

            try
            {
                if (File.Exists(roastsFilePath))
                {
                    _roasts.AddRange(File.ReadAllLines(roastsFilePath).Where(line => !string.IsNullOrWhiteSpace(line)));
                }
            }
            catch (Exception ex)
            {
                // Silently fail logging to debug, don't annoy user
                Debug.WriteLine($"Could not load roasts from file: {ex.Message}");
            }

            // FIX: If no roasts found, use a clean, professional title instead of "Project Scraper"
            if (_roasts.Count == 0)
            {
                _roasts.Add("Code Crammer");
            }
        }

        private List<string> SafeGetFiles(string path)
        {
            var files = new List<string>();
            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            queue.Enqueue(path);
            visited.Add(Path.GetFullPath(path));

            while (queue.Count > 0)
            {
                var currentDir = queue.Dequeue();
                try
                {
                    var dirFiles = Directory.GetFiles(currentDir);
                    foreach (var file in dirFiles)
                    {
                        if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                            file.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
                        {
                            files.Add(file);
                        }
                    }

                    foreach (var dir in Directory.GetDirectories(currentDir))
                    {
                        string fullDirPath = Path.GetFullPath(dir);
                        if (visited.Contains(fullDirPath)) continue;

                        string dirName = Path.GetFileName(dir);
                        bool isExcluded = _excludeFolders.Any(ex =>
                            dirName.Equals(ex.Trim('\\'), StringComparison.OrdinalIgnoreCase) ||
                            dir.IndexOf(ex, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (!isExcluded)
                        {
                            visited.Add(fullDirPath);
                            queue.Enqueue(dir);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine($"Skipped locked folder: {currentDir}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error scanning {currentDir}: {ex.Message}");
                }
            }
            return files;
        }

        private string GetSafeRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrEmpty(relativeTo) || string.IsNullOrEmpty(path)) return path;
            try
            {
                if (!Path.GetPathRoot(relativeTo).Equals(Path.GetPathRoot(path), StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
                return Path.GetRelativePath(relativeTo, path);
            }
            catch
            {
                return path;
            }
        }

        private void SafeInvoke(Action action)
        {
            if (this.Disposing || this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!this.Disposing && !this.IsDisposed) action();
                }));
            }
            else
            {
                action();
            }
        }

        #endregion " Helper Functions "
    }
}