namespace Code_Crammer.Data.Forms
{
    partial class frmAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAbout));
            picIcon = new PictureBox();
            rtbInfo = new RichTextBox();
            lblApp = new Label();
            btnOK = new Button();
            btnUpdate = new Button();
            ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
            SuspendLayout();

            picIcon.Image = (Image)resources.GetObject("picIcon.Image");
            picIcon.Location = new Point(12, 12);
            picIcon.Name = "picIcon";
            picIcon.Size = new Size(150, 150);
            picIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            picIcon.TabIndex = 0;
            picIcon.TabStop = false;

            rtbInfo.BackColor = SystemColors.Control;
            rtbInfo.BorderStyle = BorderStyle.None;
            rtbInfo.Location = new Point(168, 12);
            rtbInfo.Name = "rtbInfo";
            rtbInfo.ReadOnly = true;
            rtbInfo.Size = new Size(332, 150);
            rtbInfo.TabIndex = 1;
            rtbInfo.Text = resources.GetString("rtbInfo.Text");
            rtbInfo.LinkClicked += rtbInfo_LinkClicked;

            lblApp.Location = new Point(12, 175);
            lblApp.Name = "lblApp";
            lblApp.Size = new Size(150, 54);
            lblApp.TabIndex = 2;
            lblApp.Text = "G&&G Designs\r\n\r\nCode Crammer v1.1.0";
            lblApp.TextAlign = ContentAlignment.TopCenter;

            btnOK.Location = new Point(425, 206);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 3;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;

            btnUpdate.BackColor = Color.FromArgb(224, 224, 224);
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Image = (Image)resources.GetObject("btnUpdate.Image");
            btnUpdate.Location = new Point(425, 116);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(72, 65);
            btnUpdate.TabIndex = 4;
            btnUpdate.UseVisualStyleBackColor = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(510, 239);
            Controls.Add(btnUpdate);
            Controls.Add(btnOK);
            Controls.Add(lblApp);
            Controls.Add(rtbInfo);
            Controls.Add(picIcon);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmAbout";
            Padding = new Padding(10);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "About Code Crammer";
            ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
            ResumeLayout(false);

        }
        #endregion

        private PictureBox picIcon;
        private RichTextBox rtbInfo;
        private Label lblApp;
        private Button btnOK;
        private Button btnUpdate;
    }
}