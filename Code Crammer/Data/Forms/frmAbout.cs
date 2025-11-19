using System.Diagnostics;
using Code_Crammer.Data;

namespace Code_Crammer.Data.Forms
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
        }

        private void rtbInfo_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.LinkText)) return;
            try
            {
                Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                await UpdateChecker.CheckForUpdatesAsync(false);
            }
            finally
            {
                if (!this.IsDisposed)
                {
                    btnUpdate.Enabled = true;
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}