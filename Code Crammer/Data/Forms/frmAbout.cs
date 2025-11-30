#nullable enable

using Code_Crammer.Data.Classes.Services;
using System.Diagnostics;
using System.Reflection;

namespace Code_Crammer.Data.Forms
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
            this.btnUpdate.Click += btnUpdate_Click;
            this.rtbInfo.LinkClicked += rtbInfo_LinkClicked;
            this.btnOK.Click += btnOK_Click;
            this.Load += frmAbout_Load;
        }

        private void frmAbout_Load(object? sender, EventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                string verString = $"v{version.Major}.{version.Minor}.{version.Build}";
                lblApp.Text = $"G&&G Designs\r\n\r\nCode Crammer {verString}";
                try
                {
                    string currentText = rtbInfo.Text;
                    string newText = System.Text.RegularExpressions.Regex.Replace(currentText, @"v\d+\.\d+\.\d+", verString);
                    rtbInfo.Text = newText;
                }
                catch
                {
                }
            }
        }

        private void rtbInfo_LinkClicked(object? sender, LinkClickedEventArgs e)
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

        private async void btnUpdate_Click(object? sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            string originalText = btnUpdate.Text;
            btnUpdate.Text = "Checking...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var result = await UpdateChecker.CheckForUpdatesAsync();

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    MessageBox.Show($"Update check failed:\r\n{result.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (result.HasUpdate)
                {
                    var dialogResult = MessageBox.Show(
                        $"A new version is available!\r\n\r\n" +
                        $"Current Version: {result.LocalVersion}\r\n" +
                        $"New Version: {result.RemoteVersion}\r\n\r\n" +
                        "Would you like to open the download page?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (dialogResult == DialogResult.Yes && !string.IsNullOrEmpty(result.DownloadUrl))
                    {
                        Process.Start(new ProcessStartInfo(result.DownloadUrl) { UseShellExecute = true });
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"You are running the latest version.\r\n\r\n" +
                        $"Local: {result.LocalVersion}\r\n" +
                        $"Remote: {result.RemoteVersion ?? "Unknown"}",
                        "Up to Date",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!this.IsDisposed)
                {
                    btnUpdate.Enabled = true;
                    btnUpdate.Text = originalText;
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}