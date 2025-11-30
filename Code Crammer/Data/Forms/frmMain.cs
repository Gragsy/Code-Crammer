#nullable enable

using Code_Crammer.Data.Classes.Components;
using Code_Crammer.Data.Classes.Core;
using Code_Crammer.Data.Classes.Models;
using Code_Crammer.Data.Classes.Services;
using Code_Crammer.Data.Classes.Skin;
using Code_Crammer.Data.Classes.Utilities;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace Code_Crammer.Data.Forms_Classes
{
    public partial class frmMain : Form
    {
        #region " Class-Level Declarations "

        private const int TIMER_INTERVAL_REBUILD = 500;
        private readonly Random _random = new Random();

        private readonly List<string> _roasts = new List<string>
        {
            "Code Crammer",
            "Code Crammer - Now with 10% less bugs!",
            "Code Crammer - AI's best friend",
            "Code Crammer - Compressing your genius"
        };

        private readonly TokenCounter _tokenCounter = new TokenCounter();
        private readonly UndoRedoManager _undoRedoManager = new UndoRedoManager();
        private readonly ProfileUiManager _profileUiManager = new ProfileUiManager();
        private readonly OptionsUiManager _optionsUiManager;
        private TreeViewManager? _treeViewManager;
        private ContextMenuHandler? _contextMenuHandler;
        private OutputHandler? _outputHandler;

        private List<string> _cachedProjectFiles = new List<string>();
        private string _baseFormTitle = string.Empty;
        private ScraperOptions _currentOptions = new ScraperOptions();
        private string? _currentProfilePath;
        private CancellationTokenSource? _scraperCTS;
        private bool _suppressCheckEvents = false;
        private bool _rebuildTreeRequired = false;
        private readonly SemaphoreSlim _treeSemaphore = new SemaphoreSlim(1, 1);
        private bool _isRebuilding = false;
        private bool _isClosing = false;
        private HashSet<string>? _lastCheckedFiles;
        private HashSet<string>? _lastExpandedNodes;
        private bool _isUndoingRedoing = false;
        private ToolStripMenuItem? _selectedHistoryItem;
        private bool _isRightClickingHistory = false;
        private AppState? _lastKnownGoodState;

        private readonly System.Windows.Forms.Timer tmrAutoSave = new System.Windows.Forms.Timer();
        private const int EM_SETCUEBANNER = 0x1501;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);

        #endregion " Class-Level Declarations "

        #region " Initialization "

        public frmMain()
        {
            InitializeComponent();

            _treeViewManager = new TreeViewManager(tvwFiles, (msg, color) => Log(msg, color));
            _contextMenuHandler = new ContextMenuHandler((msg, color) => Log(msg, color), GetCurrentOptions, _treeViewManager);
            _outputHandler = new OutputHandler((msg, color) => Log(msg, color));
            _optionsUiManager = new OptionsUiManager(clbFileTypes, clbProcessing, clbOutput, (msg, color) => Log(msg, color));

            BindEvents();
        }

        private void BindEvents()
        {
            this.Load += frmMain_Load;
            this.Shown += frmMain_Shown;
            this.FormClosing += frmMain_FormClosing;

            SetDoubleBuffered(tvwFiles);

            btnSelectFolder.Click -= btnSelectFolder_Click;
            btnSelectFolder.Click += btnSelectFolder_Click;

            btnGenerate.Click -= btnGenerate_Click;
            btnGenerate.Click += btnGenerate_Click;

            btnEditMessage.Click -= btnEditMessage_Click;
            btnEditMessage.Click += btnEditMessage_Click;

            btnSave.Click -= btnSave_Click;
            btnSave.Click += btnSave_Click;

            btnSaveAs.Click -= btnSaveAs_Click;
            btnSaveAs.Click += btnSaveAs_Click;

            btnHelp.Click -= btnHelp_Click;
            btnHelp.Click += btnHelp_Click;

            btnUndo.Click -= btnUndo_Click;
            btnUndo.Click += btnUndo_Click;

            btnRedo.Click -= btnRedo_Click;
            btnRedo.Click += btnRedo_Click;

            mnuDefaultSettings.Click -= btnDefault_Click;
            mnuDefaultSettings.Click += btnDefault_Click;

            mnuSkinLight.Click -= mnuSkinLight_Click;
            mnuSkinLight.Click += mnuSkinLight_Click;

            mnuSkinDark.Click -= mnuSkinDark_Click;
            mnuSkinDark.Click += mnuSkinDark_Click;

            if (mnuShowToolTips != null)
            {
                mnuShowToolTips.Click -= mnuShowToolTips_Click;
                mnuShowToolTips.Click += mnuShowToolTips_Click;
            }

            if (mnuShowTokens != null)
            {
                mnuShowTokens.Click -= mnuShowTokens_Click;
                mnuShowTokens.Click += mnuShowTokens_Click;
            }

            txtSearch.KeyDown -= txtSearch_KeyDown;
            txtSearch.KeyDown += txtSearch_KeyDown;

            txtSearch.TextChanged -= txtSearch_TextChanged;
            txtSearch.TextChanged += txtSearch_TextChanged;

            tmrRepopulate.Tick -= tmrRepopulate_Tick;
            tmrRepopulate.Tick += tmrRepopulate_Tick;

            tmrAutoSave.Tick -= tmrAutoSave_Tick;
            tmrAutoSave.Tick += tmrAutoSave_Tick;

            tvwFiles.AfterCheck -= tvwFiles_AfterCheck;
            tvwFiles.AfterCheck += tvwFiles_AfterCheck;

            tvwFiles.MouseDown -= tvwFiles_MouseDown;
            tvwFiles.MouseDown += tvwFiles_MouseDown;

            tvwFiles.DragEnter -= tvwFiles_DragEnter;
            tvwFiles.DragEnter += tvwFiles_DragEnter;

            tvwFiles.DragDrop -= tvwFiles_DragDrop;
            tvwFiles.DragDrop += tvwFiles_DragDrop;

            this.DragEnter -= tvwFiles_DragEnter;
            this.DragEnter += tvwFiles_DragEnter;

            this.DragDrop -= tvwFiles_DragDrop;
            this.DragDrop += tvwFiles_DragDrop;

            _optionsUiManager.BindEvents(clbOptions_ItemCheck, TipTop);

            ctmTreeView.Opening += (s, e) => _contextMenuHandler?.HandleTreeViewOpening(tvwFiles, mnuView, e);

            mnuView.Click += (s, e) => _contextMenuHandler?.ViewSelectedFile(tvwFiles.SelectedNode, txtFolderPath.Text);
            mnuExplorer.Click += (s, e) => _contextMenuHandler?.ExploreSelectedItem(tvwFiles.SelectedNode, txtFolderPath.Text);
            mnuConvertToText.Click += (s, e) => _contextMenuHandler?.ConvertToText();

            mnuReset.Click += (s, e) => { SetGlobalCheckState(true); Log("File selection has been reset.", Color.LimeGreen); };
            mnuCollapseUnused.Click += (s, e) => _contextMenuHandler?.CollapseUnusedNodes(tvwFiles);
            mnuOpenFolder.Click += (s, e) => _contextMenuHandler?.OpenSelectedFolder(tvwFiles.SelectedNode, txtFolderPath.Text);

            mnuSelectAllParent.Click += (s, e) => { if (tvwFiles.SelectedNode != null) SetRecursiveCheckState(tvwFiles.SelectedNode.Parent ?? tvwFiles.SelectedNode, true); };
            mnuDeselectAllParent.Click += (s, e) => { if (tvwFiles.SelectedNode != null) SetRecursiveCheckState(tvwFiles.SelectedNode.Parent ?? tvwFiles.SelectedNode, false); };
            mnuExpandAllParent.Click += (s, e) => { if (tvwFiles.SelectedNode != null) (tvwFiles.SelectedNode.Parent ?? tvwFiles.SelectedNode).ExpandAll(); };
            mnuCollapseAllParent.Click += (s, e) => { if (tvwFiles.SelectedNode != null) (tvwFiles.SelectedNode.Parent ?? tvwFiles.SelectedNode).Collapse(); };

            mnuSelectAllGlobal.Click += (s, e) => SetGlobalCheckState(true);
            mnuDeselectAllGlobal.Click += (s, e) => SetGlobalCheckState(false);
            mnuExpandAllGlobal.Click += (s, e) => { tvwFiles.ExpandAll(); Log("All folders have been expanded.", Color.LimeGreen); };
            mnuCollapseAllGlobal.Click += (s, e) => { tvwFiles.CollapseAll(); Log("All folders have been collapsed.", Color.LimeGreen); };

            mnuCram.Click += (s, e) => RunSafeAsync(async () =>
            {
                if (_contextMenuHandler != null)
                {
                    await _contextMenuHandler.CramSelectionAsync(tvwFiles.SelectedNode, txtFolderPath.Text, this.Cursor, c => this.Cursor = c);
                }
            }, "Context Menu Cram");

            mnuResetToDefault.Click -= mnuResetToDefault_Click;
            mnuResetToDefault.Click += mnuResetToDefault_Click;

            btnHistory.DropDown.Closing -= HistoryDropDown_Closing;
            btnHistory.DropDown.Closing += HistoryDropDown_Closing;

            mnuDeleteHistoryItem.Click -= mnuDeleteHistoryItem_Click;
            mnuDeleteHistoryItem.Click += mnuDeleteHistoryItem_Click;

            mnuCopy.Click += (s, e) => RunSafeAsync(async () =>
            {
                if (rtbLog.TextLength > 0 && _outputHandler != null)
                    await _outputHandler.SafeCopyToClipboardAsync(rtbLog.Text);
            }, "Copy Log");

            mnuCopySelected.Click += (s, e) => RunSafeAsync(async () =>
            {
                if (rtbLog.SelectionLength > 0 && _outputHandler != null)
                    await _outputHandler.SafeCopyToClipboardAsync(rtbLog.SelectedText);
            }, "Copy Selected Log");

            mnuClear.Click += (s, e) => rtbLog.Clear();
        }

        #endregion " Initialization "

        #region " Form Events "

        private void frmMain_Load(object? sender, EventArgs e)
        {
            if (Properties.Settings.Default.CallUpgrade)
            {
                try
                {
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.CallUpgrade = false;
                    Properties.Settings.Default.Save();
                }
                catch (System.Configuration.ConfigurationErrorsException)
                {

                    Debug.WriteLine("Settings corrupted, resetting to defaults.");
                    Properties.Settings.Default.Reset();
                    Properties.Settings.Default.CallUpgrade = false;
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {

                    Debug.WriteLine($"Settings upgrade failed (Transient): {ex.Message}");

                    Properties.Settings.Default.CallUpgrade = false;
                }
            }

            LoadRoastsFromFile();
            LanguageManager.OnError = (msg) => Log(msg, Color.Red);
            ProfileManager.OnError = (msg) => Log(msg, Color.Red);
            LanguageManager.Initialize();

            int roastIndex = _random.Next(0, _roasts.Count);
            _baseFormTitle = _roasts[roastIndex];
            UpdateFormTitle();

            LoadSettings();
            PopulateProfilesDropdown();
            PopulateHistoryDropdown();

            tmrRepopulate.Interval = TIMER_INTERVAL_REBUILD;
            lblTokenCount.Text = "Approx. Tokens: 0";
            tmrAutoSave.Interval = 30000;

            btnSettings.DropDown.ImageScalingSize = new Size(16, 16);
            if (mnuShowToolTips != null) mnuShowToolTips.ImageScaling = ToolStripItemImageScaling.None;
            if (mnuSkinLight != null) mnuSkinLight.ImageScaling = ToolStripItemImageScaling.None;
            if (mnuSkinDark != null) mnuSkinDark.ImageScaling = ToolStripItemImageScaling.None;
            if (mnuShowTokens != null) mnuShowTokens.ImageScaling = ToolStripItemImageScaling.None;

            btnSettings.DropDownDirection = ToolStripDropDownDirection.Left;
            SendMessage(txtSearch.Control.Handle, EM_SETCUEBANNER, 0, "Search...");

            TipTop.SetToolTip(btnSelectFolder, TooltipContent.GetTooltip("btnSelectFolder"));
            TipTop.SetToolTip(btnGenerate, TooltipContent.GetTooltip("btnGenerate"));
            TipTop.SetToolTip(txtFolderPath, TooltipContent.GetTooltip("txtFolderPath"));
            TipTop.SetToolTip(lblTokenCount, TooltipContent.GetTooltip("lblTokenCount"));

            btnUndo.ToolTipText = TooltipContent.GetTooltip("btnUndo");
            btnRedo.ToolTipText = TooltipContent.GetTooltip("btnRedo");
            btnHistory.ToolTipText = TooltipContent.GetTooltip("btnHistory");
            btnEditMessage.ToolTipText = TooltipContent.GetTooltip("btnEditMessage");
            mnuDefaultSettings.ToolTipText = TooltipContent.GetTooltip("btnDefault");
            btnSave.ToolTipText = TooltipContent.GetTooltip("btnSave");
            btnSaveAs.ToolTipText = TooltipContent.GetTooltip("btnSaveAs");
            btnHelp.ToolTipText = TooltipContent.GetTooltip("btnHelp");
            txtSearch.ToolTipText = TooltipContent.GetTooltip("txtSearch");

            mnuView.ToolTipText = TooltipContent.GetTooltip("mnuView");
            mnuExplorer.ToolTipText = TooltipContent.GetTooltip("mnuExplorer");
            mnuConvertToText.ToolTipText = TooltipContent.GetTooltip("mnuConvertToText");
            mnuCram.ToolTipText = TooltipContent.GetTooltip("mnuCram");

            if (_treeViewManager != null)
            {
                _lastKnownGoodState = CaptureCurrentState();
            }
        }

        private async void frmMain_Shown(object? sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtFolderPath.Text) && Directory.Exists(txtFolderPath.Text))
                {
                    btnGenerate.Enabled = true;
                    await PopulateFileTreeAsync();
                }
                else
                {
                    btnGenerate.Enabled = false;
                    txtFolderPath.Text = string.Empty;
                    btnSelectFolder.Focus();
                }
            }
            catch (Exception ex)
            {
                Log($"Error during startup population: {ex.Message}", Color.Red);
            }
        }

        private void frmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                _scraperCTS?.Cancel();
                tmrAutoSave?.Stop();

                SaveSettingsSync();

                CleanUpResources();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }
        }

        private void CleanUpResources()
        {
            try
            {
                tmrAutoSave?.Stop();
                tmrAutoSave?.Dispose();
            }
            catch { }

            try
            {
                _tokenCounter?.Dispose();
            }
            catch { }

            try
            {
                _scraperCTS?.Cancel();
                _scraperCTS?.Dispose();
            }
            catch { }

            try
            {
                _treeSemaphore?.Dispose();
            }
            catch { }

            try
            {
                if (btnHistory != null && btnHistory.HasDropDownItems)
                {
                    foreach (ToolStripItem item in btnHistory.DropDownItems)
                    {
                        item.Dispose();
                    }
                    btnHistory.DropDownItems.Clear();
                }
            }
            catch { }
        }

        #endregion " Form Events "

        #region " UI Control Events "

        private async void btnSelectFolder_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the root folder of your solution";
                dialog.UseDescriptionForTitle = true;
                if (!string.IsNullOrEmpty(txtFolderPath.Text) && Directory.Exists(txtFolderPath.Text))
                {
                    dialog.SelectedPath = txtFolderPath.Text;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ProjectBuilder.ClearCache();
                    _lastExpandedNodes?.Clear();
                    txtFolderPath.Text = dialog.SelectedPath;
                    btnGenerate.Enabled = true;
                    Log("Solution folder selected: " + dialog.SelectedPath, Color.Cyan);
                    await SaveSettingsAsync();
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

        private void clbOptions_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (!_isUndoingRedoing)
            {
                var state = CaptureCurrentState();
                PushUndoState(state);
            }
            if (_isRebuilding) return;
            this.BeginInvoke(new Action(() =>
            {
                SetMainControlsEnabled(false);
                RequestSettingsSave();
                _rebuildTreeRequired = true;
                tmrRepopulate.Stop();
                tmrRepopulate.Start();
            }));
        }

        private async void btnDefault_Click(object? sender, EventArgs e)
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

        private void btnEditMessage_Click(object? sender, EventArgs e)
        {
            using (var frm = new frmMessageEditor())
            {
                frm.ShowDialog();
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
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

        private void btnSaveAs_Click(object? sender, EventArgs e)
        {
            string? fileName = _profileUiManager.ShowSaveDialog();
            if (!string.IsNullOrEmpty(fileName))
            {
                SaveProfile(fileName);
            }
        }

        private void btnHelp_Click(object? sender, EventArgs e)
        {
            using (var frm = new Code_Crammer.Data.Forms.frmAbout())
            {
                frm.ShowDialog(this);
            }
        }

        private void mnuSkinLight_Click(object? sender, EventArgs e)
        {
            mnuSkinLight.Checked = true;
            mnuSkinDark.Checked = false;
            SkinManager.ApplySkin(this, SkinManager.Skin.Light);
            ApplySkinToContextMenus(SkinManager.Skin.Light);
            SettingsManager.SaveAppSkin("Light");
            RequestSettingsSave();
        }

        private void mnuSkinDark_Click(object? sender, EventArgs e)
        {
            mnuSkinDark.Checked = true;
            mnuSkinLight.Checked = false;
            SkinManager.ApplySkin(this, SkinManager.Skin.Dark);
            ApplySkinToContextMenus(SkinManager.Skin.Dark);
            SettingsManager.SaveAppSkin("Dark");
            RequestSettingsSave();
        }

        private void mnuShowToolTips_Click(object? sender, EventArgs e)
        {
            bool show = mnuShowToolTips.Checked;
            TipTop.Active = show;
            tsMenu.ShowItemToolTips = show;
            if (!show)
            {
                TipTop.SetToolTip(clbFileTypes, null);
                TipTop.SetToolTip(clbProcessing, null);
                TipTop.SetToolTip(clbOutput, null);
            }
            tmrAutoSave.Stop();
            tmrAutoSave_Tick(sender, e);
        }

        private void mnuShowTokens_Click(object? sender, EventArgs e)
        {
            RequestSettingsSave();
            if (!_isRebuilding)
            {
                SafeInvoke(async () =>
                {
                    await PopulateFileTreeAsync();
                });
            }
        }

        private async void tmrRepopulate_Tick(object? sender, EventArgs e)
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
                    LogError("Tree Repopulation", ex.Message);
                }
            }
        }

        #endregion " UI Control Events "

        #region " Core Application Logic "

        private async void btnGenerate_Click(object? sender, EventArgs e)
        {
            if (_treeViewManager == null) return;
            if (_scraperCTS != null && !_scraperCTS.IsCancellationRequested)
            {
                _scraperCTS.Cancel();
                btnGenerate.Enabled = false;
                btnGenerate.Text = "Cancelling...";
                return;
            }

            _ = SaveToHistoryAsync();
            string solutionPath = txtFolderPath.Text;
            _scraperCTS = new CancellationTokenSource();

            try
            {
                if (string.IsNullOrEmpty(solutionPath) || !Directory.Exists(solutionPath))
                {
                    Log("ERROR: Please select a valid solution folder first.", Color.Red);
                    return;
                }

                _currentOptions = GetCurrentOptions();
                var allFilesFromTree = _treeViewManager.GetAllFilePaths();
                var checkedProjects = _treeViewManager.GetCheckedTopLevelNodes();
                var selectedFiles = _treeViewManager.GetCheckedFiles();

                if (selectedFiles.Count == 0 && !_currentOptions.IncludeFolderLayout && !_currentOptions.DistillUnused && !_currentOptions.DistillUnusedHeaders)
                {
                    Log("ERROR: Please select at least one file or option.", Color.Red);
                    return;
                }

                string projectStructureString = string.Empty;
                if (_currentOptions.IncludeFolderLayout)
                {
                    projectStructureString = _treeViewManager.GetTreeAsText();
                }

                this.Cursor = Cursors.WaitCursor;
                btnGenerate.Text = "Cancel";
                rtbLog.Clear();
                Log("--- Starting Universal Project Scraping Operation ---", Color.Yellow);

                var progressHandler = new Progress<string>(progress =>
                {
                    if (!_isClosing) Log(progress);
                });

                string messageContent = LoadMessageContent();

                string resultText = await ProjectBuilder.GenerateProjectStateStringAsync(
                    solutionPath,
                    _currentOptions,
                    allFilesFromTree,
                    checkedProjects,
                    selectedFiles,
                    _cachedProjectFiles,
                    progressHandler,
                    _scraperCTS.Token,
                    projectStructureString,
                    messageContent);

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
                btnGenerate.Text = "Cram Selected";
                btnGenerate.Enabled = true;
                _scraperCTS?.Dispose();
                _scraperCTS = null;
            }
        }

        private async Task HandleScrapingSuccessAsync(string resultText)
        {
            long finalTokens = ProjectBuilder.GetApproximateTokenCount(resultText);
            lblTokenCount.Text = $"Final Count: {finalTokens:N0}";
            Log($"Final token count: {finalTokens:N0}", Color.LimeGreen);

            if (_outputHandler != null)
            {
                await _outputHandler.HandleOutputAsync(resultText, _currentOptions);
            }
        }

        #endregion " Core Application Logic "

        #region " TreeView Management "

        private async Task PopulateFileTreeAsync(bool updateTokens = true)
        {
            if (_treeViewManager == null) return;
            if (!await _treeSemaphore.WaitAsync(0))
            {
                Log("Tree rebuild already in progress, skipping...", Color.Orange);
                return;
            }

            if (tvwFiles.Nodes.Count > 0)
            {
                _lastExpandedNodes = _treeViewManager.GetExpansionState();
            }

            _isRebuilding = true;
            this.Cursor = Cursors.WaitCursor;
            SetMainControlsEnabled(false);
            try
            {
                Log("Building file tree... (This may take a moment)", Color.Yellow);
                string solutionPath = txtFolderPath.Text;
                if (string.IsNullOrEmpty(solutionPath) || !Directory.Exists(solutionPath))
                {
                    return;
                }

                var options = GetCurrentOptions();
                bool excludeMyProject = _optionsUiManager.GetCurrentOptions(false).ExcludeProjectSettings;

                _cachedProjectFiles = await _treeViewManager.PopulateFileTreeAsync(solutionPath, options, excludeMyProject, _lastCheckedFiles, _lastExpandedNodes);

                int projectCount = _cachedProjectFiles.Count;
                Log($"Found {projectCount} projects and built file tree.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                Log($"Error building file tree: {ex.Message}", Color.Red);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                _isRebuilding = false;
                SetMainControlsEnabled(true);
                _treeSemaphore.Release();

                if (updateTokens)
                {
                    UpdateTokenCountAsync();
                }
            }
        }

        private void tvwFiles_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            if (_suppressCheckEvents || _treeViewManager == null) return;
            if (e.Action == TreeViewAction.Unknown) return;
            if (_isUndoingRedoing) return;
            if (this.Disposing || this.IsDisposed) return;

            if (_lastKnownGoodState != null)
            {
                PushUndoState(_lastKnownGoodState);
            }

            _suppressCheckEvents = true;
            tvwFiles.BeginUpdate();
            try
            {
                _treeViewManager.HandleAfterCheck(e);
                _lastKnownGoodState = CaptureCurrentState();
                _lastCheckedFiles = _treeViewManager.GetCheckedFiles();
            }
            finally
            {
                tvwFiles.EndUpdate();
                _suppressCheckEvents = false;
            }
            RequestSettingsSave();
            UpdateTokenCountAsync();
        }

        private void tvwFiles_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode? clickedNode = tvwFiles.GetNodeAt(e.X, e.Y);
                if (clickedNode != null)
                {
                    tvwFiles.SelectedNode = clickedNode;
                }
            }
        }

        #endregion " TreeView Management "

        #region " Token Counting "

        private async void UpdateTokenCountAsync()
        {
            if (this.Disposing || this.IsDisposed || _treeViewManager == null) return;

            if (!await _treeSemaphore.WaitAsync(0))
            {
                return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                lblTokenCount.Text = "Calculating...";

                var progressHandler = new Progress<string>(progress =>
                {
                    if (!_isClosing && !this.IsDisposed)
                    {
                        lblTokenCount.Text = progress;
                    }
                });

                long totalTokens = await _tokenCounter.CountTokensAsync(
                    txtFolderPath.Text,
                    GetCurrentOptions(),
                    _treeViewManager,
                    _cachedProjectFiles,
                    LoadMessageContent(),
                    progressHandler);

                if (!this.IsDisposed)
                {
                    lblTokenCount.Text = $"Approx. Tokens: {totalTokens:N0}";
                }
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed)
                {
                    lblTokenCount.Text = "Error Counting";
                    Log($"Error during token count: {ex.Message}", Color.Red);
                }
            }
            finally
            {
                _treeSemaphore.Release();

                if (!this.IsDisposed)
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        #endregion " Token Counting "

        #region " State Management "

        private ScraperOptions GetCurrentOptions()
        {
            bool showTokens = mnuShowTokens != null && mnuShowTokens.Checked;
            return _optionsUiManager.GetCurrentOptions(showTokens);
        }

        #endregion " State Management "

        #region " Undo & Redo Management "

        private AppState CaptureCurrentState()
        {
            var state = new AppState();
            state.CheckedFiles = _treeViewManager?.GetCheckedFiles();
            state.Options = _optionsUiManager.CaptureState();
            return state;
        }

        private void RestoreAppState(AppState? state)
        {
            if (state == null || _treeViewManager == null) return;

            _optionsUiManager.RestoreState(state.Options);
            SetGlobalCheckState(false);
            _treeViewManager.RestoreTreeState(state.CheckedFiles);
            _lastCheckedFiles = state.CheckedFiles;
        }

        private void PushUndoState(AppState newState)
        {
            _undoRedoManager.PushState(newState);
            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateUndoRedoButtons));
                return;
            }
            btnUndo.Enabled = _undoRedoManager.CanUndo;
            btnRedo.Enabled = _undoRedoManager.CanRedo;
        }

        private void btnUndo_Click(object? sender, EventArgs e)
        {
            if (!_undoRedoManager.CanUndo) return;
            _isUndoingRedoing = true;
            tvwFiles.BeginUpdate();
            try
            {
                var currentState = CaptureCurrentState();
                var stateToRestore = _undoRedoManager.Undo(currentState);
                RestoreAppState(stateToRestore);
                Log("Undo performed.", Color.Gray);
            }
            finally
            {
                tvwFiles.EndUpdate();
                _isUndoingRedoing = false;
                UpdateUndoRedoButtons();
                UpdateTokenCountAsync();
            }
        }

        private void btnRedo_Click(object? sender, EventArgs e)
        {
            if (!_undoRedoManager.CanRedo) return;
            _isUndoingRedoing = true;
            tvwFiles.BeginUpdate();
            try
            {
                var currentState = CaptureCurrentState();
                var stateToRestore = _undoRedoManager.Redo(currentState);
                RestoreAppState(stateToRestore);
                Log("Redo performed.", Color.Gray);
            }
            finally
            {
                tvwFiles.EndUpdate();
                _isUndoingRedoing = false;
                UpdateUndoRedoButtons();
                UpdateTokenCountAsync();
            }
        }

        #endregion " Undo & Redo Management "

        #region " Profile & History Management "

        private void LoadProfile(string filePath)
        {
            var profile = ProfileManager.LoadProfile(filePath);
            if (profile == null)
            {
                Log("Failed to load profile.", Color.Red);
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
            Log($"Profile '{profileName}' loaded successfully.", Color.LimeGreen);
        }

        private void SaveProfile(string filePath)
        {
            try
            {
                var profile = GatherCurrentProfileData();
                ProfileManager.SaveProfile(filePath, profile);
                _currentProfilePath = filePath;
                UpdateFormTitle();
                PopulateProfilesDropdown();
                Log($"Profile saved successfully to '{Path.GetFileName(filePath)}'.", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                Log($"Error saving profile: {ex.Message}", Color.Red);
            }
        }

        private async void ApplyProfile(ProfileData profile)
        {
            if (profile == null || _treeViewManager == null) return;

            _optionsUiManager.UnbindEvents(clbOptions_ItemCheck);
            try
            {
                _optionsUiManager.RestoreState(profile.OptionStates);
            }
            finally
            {
                _optionsUiManager.BindEvents(clbOptions_ItemCheck, TipTop);
            }

            await PopulateFileTreeAsync(false);

            _suppressCheckEvents = true;
            tvwFiles.BeginUpdate();
            try
            {
                _treeViewManager.RestoreTreeState(profile.CheckedFiles);
                _treeViewManager.RestoreExpansionState(profile.ExpandedNodes);
            }
            finally
            {
                tvwFiles.EndUpdate();
                _suppressCheckEvents = false;
            }
            UpdateTokenCountAsync();
        }

        private ProfileData GatherCurrentProfileData()
        {
            var profile = new ProfileData();
            profile.SolutionPath = txtFolderPath.Text;
            profile.OptionStates = _optionsUiManager.CaptureState();
            profile.CheckedFiles = _treeViewManager?.GetCheckedFiles() ?? new HashSet<string>();
            profile.ExpandedNodes = _treeViewManager?.GetExpansionState() ?? new HashSet<string>();
            return profile;
        }

        private async Task SaveToHistoryAsync()
        {
            string historyFolder = PathManager.GetHistoryFolderPath();
            string solutionPath = txtFolderPath.Text;
            if (string.IsNullOrEmpty(solutionPath)) return;
            var profile = GatherCurrentProfileData();
            string projectName = new DirectoryInfo(solutionPath).Name;
            await ProfileManager.SaveHistoryAsync(historyFolder, projectName, profile);
            PopulateHistoryDropdown();
        }

        private void PopulateHistoryDropdown()
        {
            _profileUiManager.PopulateHistoryMenu(btnHistory, HistoryMenuItem_Click, HistoryItem_MouseDown);
        }

        private void PopulateProfilesDropdown()
        {
            _profileUiManager.PopulateProfilesMenu(btnLoad, ProfileMenuItem_Click, mnuBrowseProfile_Click);
        }

        private void HistoryItem_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && sender is ToolStripMenuItem item)
            {
                _isRightClickingHistory = true;
                _selectedHistoryItem = item;
                ctmHistory.Show(Cursor.Position);
            }
        }

        private void HistoryDropDown_Closing(object? sender, ToolStripDropDownClosingEventArgs e)
        {
            if (_isRightClickingHistory)
            {
                e.Cancel = true;
                _isRightClickingHistory = false;
            }
        }

        private void mnuDeleteHistoryItem_Click(object? sender, EventArgs e)
        {
            _profileUiManager.DeleteHistoryItem(_selectedHistoryItem, (msg, color) => Log(msg, color), btnHistory);
            _selectedHistoryItem = null;
        }

        private void HistoryMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is string filePath)
            {
                LoadProfile(filePath);
                Log($"Restored history from: {item.Text}", Color.Cyan);
            }
        }

        private void ProfileMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is string filePath)
            {
                LoadProfile(filePath);
            }
        }

        private void mnuBrowseProfile_Click(object? sender, EventArgs e)
        {
            string? fileName = _profileUiManager.ShowOpenDialog();
            if (!string.IsNullOrEmpty(fileName))
            {
                LoadProfile(fileName);
            }
        }

        #endregion " Profile & History Management "

        #region " Settings Management "

        private void ApplyOptionsToUI(ScraperOptions options)
        {
            _optionsUiManager.UnbindEvents(clbOptions_ItemCheck);
            try
            {
                _optionsUiManager.ApplyOptionsToUI(options);
            }
            finally
            {
                _optionsUiManager.BindEvents(clbOptions_ItemCheck, TipTop);
            }
        }

        private void LoadSettings()
        {
            var data = SettingsManager.LoadSettings();

            if (!string.IsNullOrEmpty(data.LastPath) && Directory.Exists(data.LastPath))
            {
                txtFolderPath.Text = data.LastPath;
            }
            else
            {
                txtFolderPath.Text = string.Empty;
            }

            string skinName = SettingsManager.GetAppSkin();
            var skin = skinName == "Dark" ? SkinManager.Skin.Dark : SkinManager.Skin.Light;

            if (mnuSkinLight != null) mnuSkinLight.Checked = (skin == SkinManager.Skin.Light);
            if (mnuSkinDark != null) mnuSkinDark.Checked = (skin == SkinManager.Skin.Dark);

            SkinManager.ApplySkin(this, skin);
            ApplySkinToContextMenus(skin);

            bool showTips = SettingsManager.GetShowToolTips();
            if (mnuShowToolTips != null)
            {
                mnuShowToolTips.Checked = showTips;
            }
            if (mnuShowTokens != null)
            {
                mnuShowTokens.Checked = data.Options.ShowPerFileTokens;
            }

            TipTop.Active = showTips;
            tsMenu.ShowItemToolTips = showTips;

            if (data.Session != null)
            {
                _lastCheckedFiles = data.Session.CheckedFiles;
                _lastExpandedNodes = data.Session.ExpandedNodes ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _lastCheckedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _lastExpandedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            ApplyOptionsToUI(data.Options);
        }

        private void SaveSettingsSync()
        {
            if (_treeViewManager == null) return;

            var options = GetCurrentOptions();
            if (mnuShowToolTips != null)
            {
                SettingsManager.SaveShowToolTips(mnuShowToolTips.Checked);
            }

            var sessionState = new SessionState
            {
                CheckedFiles = _treeViewManager.GetCheckedFiles(),
                ExpandedNodes = _treeViewManager.GetExpansionState()
            };
            SettingsManager.SaveSettingsAsync(options, txtFolderPath.Text, sessionState).Wait(1000);
        }

        private async Task SaveSettingsAsync()
        {
            if (_treeViewManager == null) return;

            var options = GetCurrentOptions();

            if (mnuShowToolTips != null)
            {
                SettingsManager.SaveShowToolTips(mnuShowToolTips.Checked);
            }

            var sessionState = new SessionState
            {
                CheckedFiles = _treeViewManager.GetCheckedFiles(),
                ExpandedNodes = _treeViewManager.GetExpansionState()
            };

            await SettingsManager.SaveSettingsAsync(options, txtFolderPath.Text, sessionState);
        }

        private void RequestSettingsSave()
        {
            if (_isClosing) return;
            tmrAutoSave.Stop();
            tmrAutoSave.Start();
        }

        private async void tmrAutoSave_Tick(object? sender, EventArgs e)
        {
            tmrAutoSave.Stop();
            try
            {
                await SaveSettingsAsync();
                Log("Session state auto-saved.", Color.Gray);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Auto-save failed: {ex.Message}");
                Log("Warning: Auto-save failed.", Color.Orange);
            }
        }

        #endregion " Settings Management "

        #region " Search Functionality "

        private void txtSearch_TextChanged(object? sender, EventArgs e)
        {
            if (_treeViewManager == null) return;

            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                _treeViewManager.ResetNodeColors();

                if (_lastExpandedNodes != null && _lastExpandedNodes.Count > 0)
                {
                    tvwFiles.BeginUpdate();
                    tvwFiles.CollapseAll();
                    _treeViewManager.RestoreExpansionState(_lastExpandedNodes);
                    tvwFiles.EndUpdate();
                }
            }
        }

        private void txtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string term = txtSearch.Text.Trim();

                if (string.Equals(term, "tea", StringComparison.OrdinalIgnoreCase))
                {
                    Log("☕ Brewing virtual Earl Grey... Hot.", Color.Cyan);
                    MessageBox.Show("Here is your virtual tea, Boss. ☕\n\nTake a sip, breathe, and remember: The compiler is afraid of YOU.", "G&G Designs Hospitality", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtSearch.Clear();
                    e.SuppressKeyPress = true;
                    return;
                }
                else if (string.Equals(term, "massage", StringComparison.OrdinalIgnoreCase))
                {
                    Log("💆 Initiating haptic feedback sequence...", Color.Cyan);

                    var originalLoc = this.Location;
                    var rnd = new Random();
                    for (int i = 0; i < 10; i++)
                    {
                        this.Location = new Point(originalLoc.X + rnd.Next(-5, 5), originalLoc.Y + rnd.Next(-5, 5));
                        System.Threading.Thread.Sleep(20);
                    }
                    this.Location = originalLoc;
                    Log("💆 Massage complete. Shoulders lowered?", Color.LimeGreen);
                    txtSearch.Clear();
                    e.SuppressKeyPress = true;
                    return;
                }

                if (!string.IsNullOrEmpty(term))
                {
                    _treeViewManager?.PerformSearch(term);
                }
                e.SuppressKeyPress = true;
            }
        }

        #endregion " Search Functionality "

        #region " Drag & Drop "

        private async void tvwFiles_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                if (files != null && files.Length > 0)
                {
                    string path = files[0];
                    if (File.Exists(path))
                    {
                        string ext = Path.GetExtension(path).ToLowerInvariant();
                        if (ext == ".sln" || ext == ".csproj" || ext == ".vbproj")
                        {
                            path = Path.GetDirectoryName(path) ?? string.Empty;
                        }
                        else
                        {
                            Log("Invalid file dropped. Please drop a Folder, .sln, or .csproj file.", Color.Orange);
                            return;
                        }
                    }
                    if (Directory.Exists(path))
                    {
                        txtFolderPath.Text = path;
                        ProjectBuilder.ClearCache();
                        Log($"Folder dropped: {path}", Color.Cyan);
                        await SaveSettingsAsync();
                        try
                        {
                            await PopulateFileTreeAsync();
                        }
                        catch (Exception ex)
                        {
                            Log($"Error populating file tree: {ex.Message}", Color.Red);
                        }
                    }
                    else
                    {
                        Log("Please drop a valid folder or solution file.", Color.Orange);
                    }
                }
            }
        }

        private void tvwFiles_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        #endregion " Drag & Drop "

        #region " UI Helper Methods "

        private static void SetDoubleBuffered(Control control)
        {
            if (System.Windows.Forms.SystemInformation.TerminalServerSession) return;
            System.Reflection.PropertyInfo? aProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            aProp?.SetValue(control, true, null);
        }

        private async void RunSafeAsync(Func<Task> action, string contextName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Log($"Critical Error in {contextName}: {ex.Message}", Color.Red);
                Debug.WriteLine($"[CRITICAL] {contextName}: {ex}");
            }
        }

        private void mnuResetToDefault_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Owner is ContextMenuStrip menu)
            {
                if (menu.SourceControl is CheckedListBox clb)
                {
                    _optionsUiManager.ResetGroupToDefaults(clb);
                }
            }
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            _optionsUiManager.SetControlsEnabled(enabled);
            btnSelectFolder.Enabled = enabled;
            btnGenerate.Enabled = enabled;
            btnEditMessage.Enabled = enabled;
            tsMenu.Enabled = enabled;
            tvwFiles.Enabled = enabled;
        }

        private void SetRecursiveCheckState(TreeNode node, bool isChecked)
        {
            if (_treeViewManager == null) return;

            tvwFiles.BeginUpdate();
            _suppressCheckEvents = true;
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
                _treeViewManager.UpdateParentStateFromNode(node);
            }
            finally
            {
                _suppressCheckEvents = false;
                tvwFiles.EndUpdate();
            }
            UpdateTokenCountAsync();
        }

        private void SetGlobalCheckState(bool isChecked)
        {
            tvwFiles.BeginUpdate();
            _suppressCheckEvents = true;
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
                _suppressCheckEvents = false;
                tvwFiles.EndUpdate();
            }
            UpdateTokenCountAsync();
            string action = isChecked ? "Selected" : "Deselected";
            Log($"{action} all files globally.", Color.LimeGreen);
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

        private void ApplySkinToContextMenus(SkinManager.Skin skin)
        {
            SkinManager.ApplySkin(ctmTreeView, skin);
            SkinManager.ApplySkin(ctmLog, skin);
            SkinManager.ApplySkin(ctmOptions, skin);
            SkinManager.ApplySkin(ctmHistory, skin);
        }

        #endregion " UI Helper Methods "

        #region " File & Process Operations "

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

        private void LoadRoastsFromFile()
        {
            string messagesFilePath = Path.Combine(PathManager.GetDataFolderPath(), "TitleBarMessages.txt");
            try
            {
                if (File.Exists(messagesFilePath))
                {
                    var fileRoasts = File.ReadAllLines(messagesFilePath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToList();

                    if (fileRoasts.Count > 0)
                    {
                        _roasts.Clear();
                        _roasts.AddRange(fileRoasts);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load title bar messages: {ex.Message}");
            }
        }

        #endregion " File & Process Operations "

        #region " Logging "

        private void Log(string message, Color color)
        {
            if (this.Disposing || this.IsDisposed) return;

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

        private void LogError(string context, string message)
        {
            Log($"Error in {context}: {message}", Color.Red);
        }

        #endregion " Logging "
    }
}