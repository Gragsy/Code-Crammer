namespace Code_Crammer.Data.Forms_Classes
{
    partial class frmMessageEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMessageEditor));
            tsMenu = new ToolStrip();
            btnSave = new ToolStripButton();
            rtbMessage = new RichTextBox();
            ctmMenu = new ContextMenuStrip(components);
            mnuAdd = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            mnuCut = new ToolStripMenuItem();
            mnuCopy = new ToolStripMenuItem();
            mnuPaste = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            mnuSelectAll = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            mnuDelete = new ToolStripMenuItem();
            tsMenu.SuspendLayout();
            ctmMenu.SuspendLayout();
            SuspendLayout();

            tsMenu.AutoSize = false;
            tsMenu.GripStyle = ToolStripGripStyle.Hidden;
            tsMenu.ImageScalingSize = new Size(32, 32);
            tsMenu.Items.AddRange(new ToolStripItem[] { btnSave });
            tsMenu.Location = new Point(0, 0);
            tsMenu.Name = "tsMenu";
            tsMenu.Size = new Size(920, 45);
            tsMenu.TabIndex = 0;
            tsMenu.Text = "toolStrip1";

            btnSave.AutoSize = false;
            btnSave.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnSave.Image = (Image)resources.GetObject("btnSave.Image");
            btnSave.ImageTransparentColor = Color.Magenta;
            btnSave.Margin = new Padding(5);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(42, 42);
            btnSave.ToolTipText = "Save Message";

            rtbMessage.ContextMenuStrip = ctmMenu;
            rtbMessage.Dock = DockStyle.Fill;
            rtbMessage.Location = new Point(0, 45);
            rtbMessage.Name = "rtbMessage";
            rtbMessage.Size = new Size(920, 727);
            rtbMessage.TabIndex = 1;
            rtbMessage.Text = "";

            ctmMenu.Items.AddRange(new ToolStripItem[] { mnuAdd, toolStripSeparator3, mnuCut, mnuCopy, mnuPaste, toolStripSeparator1, mnuSelectAll, toolStripSeparator2, mnuDelete });
            ctmMenu.Name = "ctmMenu";
            ctmMenu.Size = new Size(173, 154);

            mnuAdd.Name = "mnuAdd";
            mnuAdd.Size = new Size(172, 22);
            mnuAdd.Text = "Add To Dictionairy";

            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(169, 6);

            mnuCut.Name = "mnuCut";
            mnuCut.Size = new Size(172, 22);
            mnuCut.Text = "Cut";

            mnuCopy.Name = "mnuCopy";
            mnuCopy.Size = new Size(172, 22);
            mnuCopy.Text = "Copy";

            mnuPaste.Name = "mnuPaste";
            mnuPaste.Size = new Size(172, 22);
            mnuPaste.Text = "Paste";

            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(169, 6);

            mnuSelectAll.Name = "mnuSelectAll";
            mnuSelectAll.Size = new Size(172, 22);
            mnuSelectAll.Text = "Select All";

            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(169, 6);

            mnuDelete.Name = "mnuDelete";
            mnuDelete.Size = new Size(172, 22);
            mnuDelete.Text = "Delete";

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(920, 772);
            Controls.Add(rtbMessage);
            Controls.Add(tsMenu);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmMessageEditor";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmMessageEditor";
            tsMenu.ResumeLayout(false);
            tsMenu.PerformLayout();
            ctmMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ToolStrip tsMenu;
        private ToolStripButton btnSave;
        private RichTextBox rtbMessage;
        private ContextMenuStrip ctmMenu;
        private ToolStripMenuItem mnuAdd;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem mnuCut;
        private ToolStripMenuItem mnuCopy;
        private ToolStripMenuItem mnuPaste;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem mnuSelectAll;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem mnuDelete;
    }
}