namespace Code_Crammer.Data.Forms_Classes
{
    partial class frmMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            mnuGlobal = new ToolStripMenuItem();
            mnuSelectAllGlobal = new ToolStripMenuItem();
            mnuDeselectAllGlobal = new ToolStripMenuItem();
            ToolStripSeparator4 = new ToolStripSeparator();
            mnuExpandAllGlobal = new ToolStripMenuItem();
            mnuCollapseAllGlobal = new ToolStripMenuItem();
            gpbProcessing = new GroupBox();
            clbProcessing = new CheckedListBox();
            gpbFileTypes = new GroupBox();
            clbFileTypes = new CheckedListBox();
            ToolStripSeparator5 = new ToolStripSeparator();
            mnuView = new ToolStripMenuItem();
            tsMenu = new ToolStrip();
            btnSave = new ToolStripButton();
            btnSaveAs = new ToolStripButton();
            ToolStripSeparator6 = new ToolStripSeparator();
            btnLoad = new ToolStripButton();
            ddlProfiles = new ToolStripComboBox();
            btnHelp = new ToolStripButton();
            toolStripButton3 = new ToolStripSeparator();
            txtSearch = new ToolStripTextBox();
            lblSearch = new ToolStripLabel();
            ToolStripSeparator7 = new ToolStripSeparator();
            mnuConvertToText = new ToolStripMenuItem();
            lblTokenCount = new Label();
            tmrRepopulate = new System.Windows.Forms.Timer(components);
            clbOutput = new CheckedListBox();
            mnuCollapseAllParent = new ToolStripMenuItem();
            btnGenerate = new Button();
            btnEditMessage = new Button();
            ctmLog = new ContextMenuStrip(components);
            mnuCopy = new ToolStripMenuItem();
            mnuCopySelected = new ToolStripMenuItem();
            ToolStripSeparator8 = new ToolStripSeparator();
            mnuClear = new ToolStripMenuItem();
            gpbOutput = new GroupBox();
            btnDefault = new Button();
            tvwFiles = new TreeView();
            ctmTreeView = new ContextMenuStrip(components);
            mnuCollapseUnused = new ToolStripMenuItem();
            ToolStripSeparator1 = new ToolStripSeparator();
            mnuReset = new ToolStripMenuItem();
            ToolStripSeparator2 = new ToolStripSeparator();
            mnuParent = new ToolStripMenuItem();
            mnuOpenFolder = new ToolStripMenuItem();
            sepParent1 = new ToolStripSeparator();
            mnuSelectAllParent = new ToolStripMenuItem();
            mnuDeselectAllParent = new ToolStripMenuItem();
            ToolStripSeparator3 = new ToolStripSeparator();
            mnuExpandAllParent = new ToolStripMenuItem();
            rtbLog = new RichTextBox();
            txtFolderPath = new TextBox();
            btnSelectFolder = new Button();
            TipTop = new ToolTip(components);
            mnuCram = new ToolStripMenuItem();
            toolStripSeparator9 = new ToolStripSeparator();
            btnUndo = new ToolStripButton();
            btnRedo = new ToolStripButton();
            toolStripSeparator10 = new ToolStripSeparator();
            gpbProcessing.SuspendLayout();
            gpbFileTypes.SuspendLayout();
            tsMenu.SuspendLayout();
            ctmLog.SuspendLayout();
            gpbOutput.SuspendLayout();
            ctmTreeView.SuspendLayout();
            SuspendLayout();

            mnuGlobal.DropDownItems.AddRange(new ToolStripItem[] { mnuSelectAllGlobal, mnuDeselectAllGlobal, ToolStripSeparator4, mnuExpandAllGlobal, mnuCollapseAllGlobal });
            mnuGlobal.Name = "mnuGlobal";
            mnuGlobal.Size = new Size(162, 22);
            mnuGlobal.Text = "Global Actions";

            mnuSelectAllGlobal.Name = "mnuSelectAllGlobal";
            mnuSelectAllGlobal.Size = new Size(136, 22);
            mnuSelectAllGlobal.Text = "Select All";

            mnuDeselectAllGlobal.Name = "mnuDeselectAllGlobal";
            mnuDeselectAllGlobal.Size = new Size(136, 22);
            mnuDeselectAllGlobal.Text = "Deselect All";

            ToolStripSeparator4.Name = "ToolStripSeparator4";
            ToolStripSeparator4.Size = new Size(133, 6);

            mnuExpandAllGlobal.Name = "mnuExpandAllGlobal";
            mnuExpandAllGlobal.Size = new Size(136, 22);
            mnuExpandAllGlobal.Text = "Expand All";

            mnuCollapseAllGlobal.Name = "mnuCollapseAllGlobal";
            mnuCollapseAllGlobal.Size = new Size(136, 22);
            mnuCollapseAllGlobal.Text = "Collapse All";

            gpbProcessing.Controls.Add(clbProcessing);
            gpbProcessing.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gpbProcessing.Location = new Point(214, 81);
            gpbProcessing.Name = "gpbProcessing";
            gpbProcessing.Size = new Size(198, 149);
            gpbProcessing.TabIndex = 31;
            gpbProcessing.TabStop = false;
            gpbProcessing.Text = "Processing Options";

            clbProcessing.BorderStyle = BorderStyle.None;
            clbProcessing.CheckOnClick = true;
            clbProcessing.Dock = DockStyle.Fill;
            clbProcessing.Font = new Font("Segoe UI", 9F);
            clbProcessing.FormattingEnabled = true;
            clbProcessing.Items.AddRange(new object[] { "Distill Active Projects Only", "Distill Project (Bible Mode)", "Distill Unused", "Exclude Project Settings", "Remove Comments", "Sanitize Files (Recommended)", "Squish Designer Files" });
            clbProcessing.Location = new Point(3, 19);
            clbProcessing.Name = "clbProcessing";
            clbProcessing.Size = new Size(192, 127);
            clbProcessing.Sorted = true;
            clbProcessing.TabIndex = 0;

            gpbFileTypes.Controls.Add(clbFileTypes);
            gpbFileTypes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gpbFileTypes.Location = new Point(10, 81);
            gpbFileTypes.Name = "gpbFileTypes";
            gpbFileTypes.Size = new Size(198, 149);
            gpbFileTypes.TabIndex = 30;
            gpbFileTypes.TabStop = false;
            gpbFileTypes.Text = "File Types to Include";

            clbFileTypes.BorderStyle = BorderStyle.None;
            clbFileTypes.CheckOnClick = true;
            clbFileTypes.Dock = DockStyle.Fill;
            clbFileTypes.Font = new Font("Segoe UI", 9F);
            clbFileTypes.FormattingEnabled = true;
            clbFileTypes.Items.AddRange(new object[] { "Code Files", "Config Files", "Designer Files", "Include Other Files", "Json Files", "Project Files", "Resource Files" });
            clbFileTypes.Location = new Point(3, 19);
            clbFileTypes.Name = "clbFileTypes";
            clbFileTypes.Size = new Size(192, 127);
            clbFileTypes.Sorted = true;
            clbFileTypes.TabIndex = 0;

            ToolStripSeparator5.Name = "ToolStripSeparator5";
            ToolStripSeparator5.Size = new Size(159, 6);

            mnuView.Name = "mnuView";
            mnuView.Size = new Size(162, 22);
            mnuView.Text = "View In Notepad";

            tsMenu.AutoSize = false;
            tsMenu.GripStyle = ToolStripGripStyle.Hidden;
            tsMenu.ImageScalingSize = new Size(32, 32);
            tsMenu.Items.AddRange(new ToolStripItem[] { btnSave, btnSaveAs, ToolStripSeparator6, btnLoad, ddlProfiles, btnHelp, toolStripButton3, txtSearch, lblSearch, toolStripSeparator10, btnUndo, btnRedo });
            tsMenu.Location = new Point(0, 0);
            tsMenu.Name = "tsMenu";
            tsMenu.Size = new Size(1045, 45);
            tsMenu.TabIndex = 29;
            tsMenu.Text = "ToolStrip1";

            btnSave.AutoSize = false;
            btnSave.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnSave.Image = (Image)resources.GetObject("btnSave.Image");
            btnSave.ImageTransparentColor = Color.Magenta;
            btnSave.Margin = new Padding(5);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(42, 42);
            btnSave.ToolTipText = "Save Profile";

            btnSaveAs.AutoSize = false;
            btnSaveAs.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnSaveAs.Image = (Image)resources.GetObject("btnSaveAs.Image");
            btnSaveAs.ImageTransparentColor = Color.Magenta;
            btnSaveAs.Margin = new Padding(5);
            btnSaveAs.Name = "btnSaveAs";
            btnSaveAs.Size = new Size(42, 42);
            btnSaveAs.ToolTipText = "Save Profile As";

            ToolStripSeparator6.Name = "ToolStripSeparator6";
            ToolStripSeparator6.Size = new Size(6, 45);

            btnLoad.AutoSize = false;
            btnLoad.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnLoad.Image = (Image)resources.GetObject("btnLoad.Image");
            btnLoad.ImageTransparentColor = Color.Magenta;
            btnLoad.Margin = new Padding(5);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(42, 42);
            btnLoad.ToolTipText = "Open Profile";

            ddlProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlProfiles.Name = "ddlProfiles";
            ddlProfiles.Size = new Size(150, 45);
            ddlProfiles.ToolTipText = "Quick Load Profile";

            btnHelp.Alignment = ToolStripItemAlignment.Right;
            btnHelp.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnHelp.Image = (Image)resources.GetObject("btnHelp.Image");
            btnHelp.ImageTransparentColor = Color.Magenta;
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(36, 42);
            btnHelp.ToolTipText = "About";
            btnHelp.Click += btnHelp_Click;

            toolStripButton3.Alignment = ToolStripItemAlignment.Right;
            toolStripButton3.Name = "toolStripButton3";
            toolStripButton3.Size = new Size(6, 45);

            txtSearch.Alignment = ToolStripItemAlignment.Right;
            txtSearch.AutoSize = false;
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(340, 45);
            txtSearch.ToolTipText = "Filter TreeView...";

            lblSearch.Alignment = ToolStripItemAlignment.Right;
            lblSearch.AutoSize = false;
            lblSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblSearch.Image = (Image)resources.GetObject("lblSearch.Image");
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(42, 42);

            ToolStripSeparator7.Name = "ToolStripSeparator7";
            ToolStripSeparator7.Size = new Size(159, 6);

            mnuConvertToText.Name = "mnuConvertToText";
            mnuConvertToText.Size = new Size(162, 22);
            mnuConvertToText.Text = "Convert To Text";

            lblTokenCount.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblTokenCount.BackColor = Color.White;
            lblTokenCount.BorderStyle = BorderStyle.FixedSingle;
            lblTokenCount.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTokenCount.Location = new Point(618, 81);
            lblTokenCount.Name = "lblTokenCount";
            lblTokenCount.Size = new Size(414, 33);
            lblTokenCount.TabIndex = 28;
            lblTokenCount.Text = "0";
            lblTokenCount.TextAlign = ContentAlignment.MiddleCenter;

            tmrRepopulate.Interval = 500;

            clbOutput.BorderStyle = BorderStyle.None;
            clbOutput.CheckOnClick = true;
            clbOutput.Dock = DockStyle.Fill;
            clbOutput.Font = new Font("Segoe UI", 9F);
            clbOutput.FormattingEnabled = true;
            clbOutput.Items.AddRange(new object[] { "Copy To Clipboard", "Create File", "Include Message", "Include Project Structure", "Open File On Completion", "Open Folder On Completion", "Show Per-File Token Counts" });
            clbOutput.Location = new Point(3, 19);
            clbOutput.Name = "clbOutput";
            clbOutput.Size = new Size(188, 127);
            clbOutput.Sorted = true;
            clbOutput.TabIndex = 0;

            mnuCollapseAllParent.Name = "mnuCollapseAllParent";
            mnuCollapseAllParent.Size = new Size(183, 22);
            mnuCollapseAllParent.Text = "Collapse All";

            btnGenerate.Enabled = false;
            btnGenerate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnGenerate.Location = new Point(389, 236);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(220, 22);
            btnGenerate.TabIndex = 23;
            btnGenerate.Text = "Generate Project State File";
            btnGenerate.UseVisualStyleBackColor = true;

            btnEditMessage.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnEditMessage.Location = new Point(7, 235);
            btnEditMessage.Name = "btnEditMessage";
            btnEditMessage.Size = new Size(107, 23);
            btnEditMessage.TabIndex = 26;
            btnEditMessage.Text = "Edit Message";
            btnEditMessage.UseVisualStyleBackColor = true;

            ctmLog.Items.AddRange(new ToolStripItem[] { mnuCopy, mnuCopySelected, ToolStripSeparator8, mnuClear });
            ctmLog.Name = "ctmLog";
            ctmLog.Size = new Size(150, 76);

            mnuCopy.Name = "mnuCopy";
            mnuCopy.Size = new Size(149, 22);
            mnuCopy.Text = "Copy All";

            mnuCopySelected.Name = "mnuCopySelected";
            mnuCopySelected.Size = new Size(149, 22);
            mnuCopySelected.Text = "Copy Selected";

            ToolStripSeparator8.Name = "ToolStripSeparator8";
            ToolStripSeparator8.Size = new Size(146, 6);

            mnuClear.Name = "mnuClear";
            mnuClear.Size = new Size(149, 22);
            mnuClear.Text = "Clear Log";

            gpbOutput.Controls.Add(clbOutput);
            gpbOutput.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gpbOutput.Location = new Point(418, 81);
            gpbOutput.Name = "gpbOutput";
            gpbOutput.Size = new Size(194, 149);
            gpbOutput.TabIndex = 32;
            gpbOutput.TabStop = false;
            gpbOutput.Text = "Output & Actions";

            btnDefault.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnDefault.Location = new Point(120, 235);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(107, 23);
            btnDefault.TabIndex = 25;
            btnDefault.Text = "Default Settings";
            btnDefault.UseVisualStyleBackColor = true;

            tvwFiles.AllowDrop = true;
            tvwFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tvwFiles.CheckBoxes = true;
            tvwFiles.ContextMenuStrip = ctmTreeView;
            tvwFiles.Location = new Point(618, 117);
            tvwFiles.Name = "tvwFiles";
            tvwFiles.Size = new Size(414, 555);
            tvwFiles.TabIndex = 27;

            ctmTreeView.Items.AddRange(new ToolStripItem[] { mnuCollapseUnused, ToolStripSeparator1, mnuReset, ToolStripSeparator2, mnuParent, mnuGlobal, ToolStripSeparator5, mnuView, ToolStripSeparator7, mnuConvertToText, toolStripSeparator9, mnuCram });
            ctmTreeView.Name = "ContextMenuStrip1";
            ctmTreeView.Size = new Size(163, 188);

            mnuCollapseUnused.Name = "mnuCollapseUnused";
            mnuCollapseUnused.Size = new Size(162, 22);
            mnuCollapseUnused.Text = "Collapse Unused";

            ToolStripSeparator1.Name = "ToolStripSeparator1";
            ToolStripSeparator1.Size = new Size(159, 6);

            mnuReset.Name = "mnuReset";
            mnuReset.Size = new Size(162, 22);
            mnuReset.Text = "Reset";

            ToolStripSeparator2.Name = "ToolStripSeparator2";
            ToolStripSeparator2.Size = new Size(159, 6);

            mnuParent.DropDownItems.AddRange(new ToolStripItem[] { mnuOpenFolder, sepParent1, mnuSelectAllParent, mnuDeselectAllParent, ToolStripSeparator3, mnuExpandAllParent, mnuCollapseAllParent });
            mnuParent.Name = "mnuParent";
            mnuParent.Size = new Size(162, 22);
            mnuParent.Text = "Parent Actions";

            mnuOpenFolder.Name = "mnuOpenFolder";
            mnuOpenFolder.Size = new Size(183, 22);
            mnuOpenFolder.Text = "Open Folder";

            sepParent1.Name = "sepParent1";
            sepParent1.Size = new Size(180, 6);

            mnuSelectAllParent.Name = "mnuSelectAllParent";
            mnuSelectAllParent.Size = new Size(183, 22);
            mnuSelectAllParent.Text = "Select All Children";

            mnuDeselectAllParent.Name = "mnuDeselectAllParent";
            mnuDeselectAllParent.Size = new Size(183, 22);
            mnuDeselectAllParent.Text = "Deselect All Children";

            ToolStripSeparator3.Name = "ToolStripSeparator3";
            ToolStripSeparator3.Size = new Size(180, 6);

            mnuExpandAllParent.Name = "mnuExpandAllParent";
            mnuExpandAllParent.Size = new Size(183, 22);
            mnuExpandAllParent.Text = "Expand All";

            rtbLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            rtbLog.BackColor = SystemColors.InfoText;
            rtbLog.ContextMenuStrip = ctmLog;
            rtbLog.Font = new Font("Consolas", 9F);
            rtbLog.ForeColor = Color.LimeGreen;
            rtbLog.Location = new Point(10, 264);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(602, 408);
            rtbLog.TabIndex = 24;
            rtbLog.Text = "";

            txtFolderPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFolderPath.BackColor = Color.White;
            txtFolderPath.Location = new Point(166, 53);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.ReadOnly = true;
            txtFolderPath.Size = new Size(866, 23);
            txtFolderPath.TabIndex = 22;

            btnSelectFolder.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSelectFolder.Location = new Point(10, 52);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(150, 23);
            btnSelectFolder.TabIndex = 21;
            btnSelectFolder.Text = "Select Solution Folder...";
            btnSelectFolder.UseVisualStyleBackColor = true;

            mnuCram.Name = "mnuCram";
            mnuCram.Size = new Size(162, 22);
            mnuCram.Text = "Cram";

            toolStripSeparator9.Name = "toolStripSeparator9";
            toolStripSeparator9.Size = new Size(159, 6);

            btnUndo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnUndo.Image = (Image)resources.GetObject("btnUndo.Image");
            btnUndo.ImageTransparentColor = Color.Magenta;
            btnUndo.Name = "btnUndo";
            btnUndo.Size = new Size(36, 42);
            btnUndo.ToolTipText = "Undo";

            btnRedo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnRedo.Image = (Image)resources.GetObject("btnRedo.Image");
            btnRedo.ImageTransparentColor = Color.Magenta;
            btnRedo.Name = "btnRedo";
            btnRedo.Size = new Size(36, 42);
            btnRedo.ToolTipText = "Redo";

            toolStripSeparator10.Name = "toolStripSeparator10";
            toolStripSeparator10.Size = new Size(6, 45);

            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1045, 678);
            Controls.Add(gpbProcessing);
            Controls.Add(gpbFileTypes);
            Controls.Add(tsMenu);
            Controls.Add(lblTokenCount);
            Controls.Add(btnGenerate);
            Controls.Add(btnEditMessage);
            Controls.Add(gpbOutput);
            Controls.Add(btnDefault);
            Controls.Add(tvwFiles);
            Controls.Add(rtbLog);
            Controls.Add(txtFolderPath);
            Controls.Add(btnSelectFolder);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Code Crammer";
            gpbProcessing.ResumeLayout(false);
            gpbFileTypes.ResumeLayout(false);
            tsMenu.ResumeLayout(false);
            tsMenu.PerformLayout();
            ctmLog.ResumeLayout(false);
            gpbOutput.ResumeLayout(false);
            ctmTreeView.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStripMenuItem mnuGlobal;
        private ToolStripMenuItem mnuSelectAllGlobal;
        private ToolStripMenuItem mnuDeselectAllGlobal;
        private ToolStripSeparator ToolStripSeparator4;
        private ToolStripMenuItem mnuExpandAllGlobal;
        private ToolStripMenuItem mnuCollapseAllGlobal;
        private GroupBox gpbProcessing;
        private CheckedListBox clbProcessing;
        private GroupBox gpbFileTypes;
        private CheckedListBox clbFileTypes;
        private ToolStripSeparator ToolStripSeparator5;
        private ToolStripMenuItem mnuView;
        private ToolStrip tsMenu;
        private ToolStripSeparator ToolStripSeparator6;
        private ToolStripButton btnSave;
        private ToolStripButton btnSaveAs;
        private ToolStripComboBox ddlProfiles;
        private ToolStripLabel lblSearch;
        private ToolStripSeparator ToolStripSeparator7;
        private ToolStripMenuItem mnuConvertToText;
        private Label lblTokenCount;
        private System.Windows.Forms.Timer tmrRepopulate;
        private CheckedListBox clbOutput;
        private ToolStripMenuItem mnuCollapseAllParent;
        private Button btnGenerate;
        private Button btnEditMessage;
        private ContextMenuStrip ctmLog;
        private ToolStripMenuItem mnuCopy;
        private ToolStripMenuItem mnuCopySelected;
        private ToolStripSeparator ToolStripSeparator8;
        private ToolStripMenuItem mnuClear;
        private GroupBox gpbOutput;
        private Button btnDefault;
        private TreeView tvwFiles;
        private ContextMenuStrip ctmTreeView;
        private ToolStripMenuItem mnuCollapseUnused;
        private ToolStripSeparator ToolStripSeparator1;
        private ToolStripMenuItem mnuReset;
        private ToolStripSeparator ToolStripSeparator2;
        private ToolStripMenuItem mnuParent;
        private ToolStripMenuItem mnuOpenFolder;
        private ToolStripSeparator sepParent1;
        private ToolStripMenuItem mnuSelectAllParent;
        private ToolStripMenuItem mnuDeselectAllParent;
        private ToolStripSeparator ToolStripSeparator3;
        private ToolStripMenuItem mnuExpandAllParent;
        private RichTextBox rtbLog;
        private TextBox txtFolderPath;
        private Button btnSelectFolder;
        private ToolTip TipTop;
        private ToolStripButton btnHelp;
        private ToolStripSeparator toolStripButton3;
        private ToolStripTextBox txtSearch;
        private ToolStripButton btnLoad;
        private ToolStripSeparator toolStripSeparator9;
        private ToolStripMenuItem mnuCram;
        private ToolStripButton btnUndo;
        private ToolStripButton btnRedo;
        private ToolStripSeparator toolStripSeparator10;
    }
}