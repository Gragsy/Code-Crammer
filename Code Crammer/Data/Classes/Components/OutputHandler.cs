#nullable enable

using Code_Crammer.Data.Classes.Models;
using System.Diagnostics;
using System.Text;

namespace Code_Crammer.Data.Classes.Components
{
    public class OutputHandler
    {
        private const long CLIPBOARD_SIZE_WARNING_BYTES = 50 * 1024 * 1024;

        private readonly Action<string, Color> _log;

        public OutputHandler(Action<string, Color> logAction)
        {
            _log = logAction;
        }

        public async Task HandleOutputAsync(string resultText, ScraperOptions options)
        {
            var successActions = new List<string>();
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string outputFilePath = "";

            _log("--- OPERATION COMPLETE ---", Color.Yellow);

            if (options.CreateFile)
            {
                try
                {
                    if (!Directory.Exists(downloadsPath))
                    {
                        downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    }
                    outputFilePath = Path.Combine(downloadsPath, "_ProjectState.txt");
                    await File.WriteAllTextAsync(outputFilePath, resultText);
                    _log($"Success! Project state file created at: {outputFilePath}", Color.LimeGreen);
                    successActions.Add($"_ProjectState.txt has been saved to your Downloads folder.");
                }
                catch (Exception ex)
                {
                    _log($"Error creating file: {ex.Message}", Color.Red);
                }
            }

            if (options.CopyToClipboard)
            {
                long byteCount = Encoding.UTF8.GetByteCount(resultText);
                bool proceedWithCopy = true;

                if (byteCount > CLIPBOARD_SIZE_WARNING_BYTES)
                {
                    var result = MessageBox.Show(
                        $"The output size is very large ({byteCount / 1024.0 / 1024.0:N2} MB).\r\n" +
                        "Copying this to the clipboard may cause the application to hang.\r\n\r\n" +
                        "Do you want to proceed with the copy?",
                        "Large Output Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        proceedWithCopy = false;
                        _log("Clipboard copy skipped by user request.", Color.Orange);
                    }
                }

                if (proceedWithCopy)
                {
                    if (await SafeCopyToClipboardAsync(resultText))
                    {
                        _log("Success! Project state has been copied to the clipboard.", Color.LimeGreen);
                        successActions.Add("Project state has been copied to the clipboard.");
                    }
                }
            }

            if (options.OpenFolderOnCompletion)
            {
                try
                {
                    Process.Start("explorer.exe", downloadsPath);
                    successActions.Add("Downloads folder has been opened.");
                }
                catch (Exception ex)
                {
                    _log($"Could not open output folder: {ex.Message}", Color.Orange);
                }
            }

            if (options.OpenFileOnCompletion && options.CreateFile && !string.IsNullOrEmpty(outputFilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(outputFilePath) { UseShellExecute = true });
                    successActions.Add("The project state file has been opened.");
                }
                catch (Exception ex)
                {
                    _log($"Could not open output file: {ex.Message}", Color.Orange);
                }
            }
            else if (options.OpenFileOnCompletion && !options.CreateFile)
            {
                _log("Skipping 'Open File On Completion' because 'Create File' was not selected.", Color.Orange);
            }

            if (successActions.Any())
            {
                MessageBox.Show("Success!\r\n\r\n" + string.Join("\r\n", successActions),
                    "Operation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        public async Task<bool> SafeCopyToClipboardAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            long byteCount = Encoding.UTF8.GetByteCount(text);
            const long HardLimitBytes = 100 * 1024 * 1024;

            if (byteCount > HardLimitBytes)
            {
                MessageBox.Show(
                    $"Output size ({byteCount / 1024.0 / 1024.0:N2} MB) exceeds the hard safety limit of 100 MB.\r\n" +
                    "Clipboard copy aborted to prevent system instability.\r\n" +
                    "Please use 'Create File' instead.",
                    "Clipboard Limit Exceeded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            if (byteCount > CLIPBOARD_SIZE_WARNING_BYTES)
            {
                var result = MessageBox.Show(
                    $"The output size is very large ({byteCount / 1024.0 / 1024.0:N2} MB).\r\n" +
                    "Copying this to the clipboard may cause the application to hang momentarily.\r\n\r\n" +
                    "Do you want to proceed?",
                    "Large Output Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No) return false;
            }

            try
            {

                bool success = await Task.Run(() =>
                {
                    bool threadResult = false;
                    var staThread = new Thread(() =>
                    {
                        try
                        {

                            Clipboard.SetDataObject(text, true, 5, 100);
                            threadResult = true;
                        }
                        catch (Exception ex)
                        {

                            System.Diagnostics.Debug.WriteLine($"STA Clipboard Error: {ex.Message}");
                        }
                    });

                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();
                    staThread.Join();
                    return threadResult;
                });

                if (success)
                {
                    _log("Success! Project state has been copied to the clipboard.", Color.LimeGreen);
                    return true;
                }
                else
                {

                    _log("Clipboard copy failed (Background STA thread error).", Color.Red);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log($"Clipboard Error: {ex.Message}", Color.Red);
                MessageBox.Show($"Could not copy to clipboard: {ex.Message}", "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}