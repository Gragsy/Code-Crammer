#nullable disable

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WeCantSpell.Hunspell;

namespace Code_Crammer.Data.Forms_Classes
{
    public partial class frmMessageEditor : Form
    {
        #region " Class Level Variables "

        private List<ToolStripItem> _dynamicMenuItems = new List<ToolStripItem>();
        private const string MessageFileName = "msg.txt";
        private readonly string MessageFilePath = Path.Combine(PathManager.GetDataFolderPath(), MessageFileName);
        private readonly HashSet<string> customWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private WordList spellChecker;
        private System.Windows.Forms.Timer spellCheckTimer = new System.Windows.Forms.Timer { Interval = 1500, Enabled = false };
        private string currentMisspelledWord = string.Empty;
        private int currentWordIndex = -1;
        private int currentWordLength = 0;
        private System.Collections.Concurrent.ConcurrentDictionary<string, bool> misspelledWords = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();
        private CancellationTokenSource _spellCheckCTS;

        private Font _boldFont;

        #endregion " Class Level Variables "

        #region " Win32 API for Margins "

        private const int EM_SETRECT = 0xB3;
        private const int WM_SETREDRAW = 0xB;
        private const int EM_HIDESELECTION = 0x43F;
        private const int EM_GETSCROLLPOS = 0x4DD;
        private const int EM_SETSCROLLPOS = 0x4DE;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, ref RECT lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref Point lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void SetInnerMargins(RichTextBox control, int left, int top, int right, int bottom)
        {
            var clientRect = control.ClientRectangle;
            var rect = new RECT
            {
                Left = clientRect.Left + left,
                Top = clientRect.Top + top,
                Right = clientRect.Right - right,
                Bottom = clientRect.Bottom - bottom
            };
            SendMessage(control.Handle, EM_SETRECT, 0, ref rect);
        }

        #endregion " Win32 API for Margins "

        public frmMessageEditor()
        {
            InitializeComponent();
            BindEvents();
        }

        private void BindEvents()
        {
            this.Load += frmMessageEditor_Load;
            this.FormClosing += frmMessageEditor_FormClosing;

            btnSave.Click += btnSave_Click;

            rtbMessage.TextChanged += RichTextBox_TextChanged;
            rtbMessage.MouseDown += RichTextBox_MouseDown;

            spellCheckTimer.Tick += spellCheckTimer_Tick;

            ctmMenu.Opening += ctmMenu_Opening;
            mnuAdd.Click += mnuAdd_Click;
            mnuCut.Click += mnuCut_Click;
            mnuCopy.Click += mnuCopy_Click;
            mnuPaste.Click += mnuPaste_Click;
            mnuDelete.Click += mnuDelete_Click;
            mnuSelectAll.Click += mnuSelectAll_Click;
        }

        #region " Form Events "

        private void frmMessageEditor_Load(object sender, EventArgs e)
        {
            InitializeSpellchecker();
            SetInnerMargins(rtbMessage, 10, 10, 10, 10);

            rtbMessage.HideSelection = false;
            LoadMessage();

            if (ctmMenu.Font != null)
            {
                _boldFont = new Font(ctmMenu.Font, FontStyle.Bold);
            }
        }

        private void frmMessageEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            spellCheckTimer.Stop();
            spellCheckTimer.Dispose();

            if (_boldFont != null)
            {
                _boldFont.Dispose();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveMessage();
            this.Close();
        }

        #endregion " Form Events "

        #region " Initialization "

        private void InitializeSpellchecker()
        {
            try
            {
                string dictionariesFolder = PathManager.GetDictionariesFolderPath();
                string affPath = Path.Combine(dictionariesFolder, "en_GB.aff");
                string dicPath = Path.Combine(dictionariesFolder, "en_GB.dic");

                if (File.Exists(affPath) && File.Exists(dicPath))
                {
                    spellChecker = WordList.CreateFromFiles(dicPath, affPath);
                    customWords.Clear();
                    string customDictPath = Path.Combine(dictionariesFolder, "custom_dictionary.txt");
                    if (File.Exists(customDictPath))
                    {
                        foreach (var word in File.ReadAllLines(customDictPath))
                        {
                            if (!string.IsNullOrWhiteSpace(word))
                            {
                                customWords.Add(word.Trim());
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Spellchecker dictionary files not found. Please ensure en_GB.aff and en_GB.dic are in the Data\\Dictionaries folder.", "Spellchecker Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing spellchecker: {ex.Message}", "Spellchecker Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion " Initialization "

        #region " Spellchecker Logic "

        private void RichTextBox_TextChanged(object sender, EventArgs e)
        {
            spellCheckTimer.Stop();
            spellCheckTimer.Start();
        }

        private void spellCheckTimer_Tick(object sender, EventArgs e)
        {
            spellCheckTimer.Stop();
            if (rtbMessage != null && !rtbMessage.IsDisposed)
            {
                CheckSpelling(rtbMessage);
            }
        }

        private async void CheckSpelling(RichTextBox rtb)
        {
            if (spellChecker == null || !rtb.Enabled || string.IsNullOrEmpty(rtb.Text)) return;

            _spellCheckCTS?.Cancel();
            _spellCheckCTS = new CancellationTokenSource();
            var token = _spellCheckCTS.Token;

            string textContent = rtb.Text;
            int currentLength = textContent.Length;

            try
            {
                var spellingResult = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var matches = Regex.Matches(textContent, @"\b[\w'’]+\b");
                    var newMisspelled = new Dictionary<string, bool>();
                    bool changesDetected = false;

                    foreach (Match wordMatch in matches)
                    {
                        token.ThrowIfCancellationRequested();

                        string key = $"{wordMatch.Index}:{wordMatch.Length}";
                        string normalizedWord = wordMatch.Value.Replace("’", "'");
                        bool isMisspelled = !(customWords.Contains(normalizedWord) || spellChecker.Check(normalizedWord));

                        newMisspelled[key] = isMisspelled;

                        if (!misspelledWords.TryGetValue(key, out bool oldStatus) || oldStatus != isMisspelled)
                        {
                            changesDetected = true;
                        }
                    }

                    if (misspelledWords.Count != newMisspelled.Count) changesDetected = true;

                    return new { Matches = matches, Misspelled = newMisspelled, HasChanges = changesDetected };
                }, token);

                if (rtb.TextLength != currentLength || rtb.Text != textContent) return;
                if (token.IsCancellationRequested) return;

                if (spellingResult.HasChanges)
                {
                    Point scrollPoint = new Point();
                    SendMessage(rtb.Handle, EM_GETSCROLLPOS, IntPtr.Zero, ref scrollPoint);

                    int currentSelectionStart = rtb.SelectionStart;
                    int currentSelectionLength = rtb.SelectionLength;

                    SendMessage(rtb.Handle, EM_HIDESELECTION, 1, 0);
                    SendMessage(rtb.Handle, WM_SETREDRAW, 0, 0);

                    try
                    {
                        misspelledWords.Clear();
                        foreach (var kvp in spellingResult.Misspelled)
                        {
                            misspelledWords[kvp.Key] = kvp.Value;
                        }

                        using (var underlineFont = new Font(rtb.Font, FontStyle.Underline))
                        using (var regularFont = new Font(rtb.Font, FontStyle.Regular))
                        {
                            foreach (Match wordMatch in spellingResult.Matches)
                            {
                                string key = $"{wordMatch.Index}:{wordMatch.Length}";
                                if (spellingResult.Misspelled.TryGetValue(key, out bool isMisspelled))
                                {
                                    rtb.Select(wordMatch.Index, wordMatch.Length);
                                    rtb.SelectionFont = isMisspelled ? underlineFont : regularFont;
                                    rtb.SelectionColor = isMisspelled ? Color.Red : rtb.ForeColor;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Spellcheck error: {ex.Message}");
                    }
                    finally
                    {
                        rtb.SelectionStart = currentSelectionStart;
                        rtb.SelectionLength = currentSelectionLength;
                        SendMessage(rtb.Handle, WM_SETREDRAW, 1, 0);
                        SendMessage(rtb.Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref scrollPoint);
                        SendMessage(rtb.Handle, EM_HIDESELECTION, 0, 0);
                        rtb.Invalidate();
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical Spellcheck error: {ex.Message}");
            }
        }

        private void RichTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (rtbMessage.SelectionLength > 0)
                {
                    int clickIndex = rtbMessage.GetCharIndexFromPosition(e.Location);
                    if (clickIndex >= rtbMessage.SelectionStart && clickIndex < rtbMessage.SelectionStart + rtbMessage.SelectionLength)
                    {
                        currentMisspelledWord = string.Empty;
                        currentWordIndex = -1;
                        return;
                    }
                }

                int charIndex = rtbMessage.GetCharIndexFromPosition(e.Location);
                int wordStart = FindWordStart(rtbMessage.Text, charIndex);
                string word = GetWordAtCharIndex(rtbMessage.Text, charIndex);

                if (!string.IsNullOrWhiteSpace(word) && spellChecker != null)
                {
                    string normalizedWord = word.Replace("’", "'");
                    if (!(customWords.Contains(normalizedWord) || spellChecker.Check(normalizedWord)))
                    {
                        currentMisspelledWord = word;
                        currentWordIndex = wordStart;
                        currentWordLength = word.Length;
                    }
                    else
                    {
                        currentMisspelledWord = string.Empty;
                        currentWordIndex = -1;
                        currentWordLength = 0;
                    }
                }
                else
                {
                    currentMisspelledWord = string.Empty;
                    currentWordIndex = -1;
                    currentWordLength = 0;
                }
            }
        }

        #endregion " Spellchecker Logic "

        #region " Context Menu "

        private void ctmMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = rtbMessage.SelectionLength > 0;
            bool hasText = rtbMessage.TextLength > 0;

            mnuCut.Enabled = hasSelection;
            mnuCopy.Enabled = hasSelection;
            mnuDelete.Enabled = hasSelection;
            mnuSelectAll.Enabled = hasText;

            try { mnuPaste.Enabled = Clipboard.ContainsText(); } catch { mnuPaste.Enabled = true; }

            foreach (var item in _dynamicMenuItems)
            {
                ctmMenu.Items.Remove(item);
                item.Dispose();
            }
            _dynamicMenuItems.Clear();

            if (!string.IsNullOrEmpty(currentMisspelledWord))
            {
                mnuAdd.Text = $"Add '{currentMisspelledWord}' to Dictionary";
                mnuAdd.Visible = true;

                var suggestions = spellChecker.Suggest(currentMisspelledWord);
                int insertIndex = 0;

                if (suggestions.Any())
                {
                    foreach (var suggestion in suggestions.Take(5))
                    {
                        var item = new ToolStripMenuItem(suggestion);

                        item.Font = _boldFont ?? ctmMenu.Font;
                        item.Click += Suggestion_Click;

                        ctmMenu.Items.Insert(insertIndex, item);
                        _dynamicMenuItems.Add(item);
                        insertIndex++;
                    }
                }
                else
                {
                    var noSuggestionsItem = new ToolStripMenuItem("(No spelling suggestions)") { Enabled = false };
                    ctmMenu.Items.Insert(insertIndex, noSuggestionsItem);
                    _dynamicMenuItems.Add(noSuggestionsItem);
                    insertIndex++;
                }

                var separator = new ToolStripSeparator();
                ctmMenu.Items.Insert(insertIndex, separator);
                _dynamicMenuItems.Add(separator);
            }
            else
            {
                mnuAdd.Visible = false;
            }
        }

        private void mnuAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentMisspelledWord)) return;

            try
            {
                string wordToAdd = currentMisspelledWord.Replace("’", "'");
                string customDictPath = Path.Combine(PathManager.GetDictionariesFolderPath(), "custom_dictionary.txt");

                File.AppendAllText(customDictPath, wordToAdd + Environment.NewLine);
                customWords.Add(wordToAdd);

                currentMisspelledWord = string.Empty;
                spellCheckTimer.Stop();
                spellCheckTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add word to custom dictionary: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Suggestion_Click(object sender, EventArgs e)
        {
            string selectedSuggestion = ((ToolStripMenuItem)sender).Text;
            if (!string.IsNullOrEmpty(currentMisspelledWord) && currentWordIndex >= 0)
            {
                try
                {
                    rtbMessage.Select(currentWordIndex, currentWordLength);
                    rtbMessage.SelectedText = selectedSuggestion;

                    currentMisspelledWord = string.Empty;
                    currentWordIndex = -1;
                    currentWordLength = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error replacing word: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void mnuCut_Click(object sender, EventArgs e)
        {
            if (rtbMessage.SelectionLength == 0) return;

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    rtbMessage.Cut();
                    return;
                }
                catch (ExternalException)
                {
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private async void mnuCopy_Click(object sender, EventArgs e)
        {
            if (rtbMessage.SelectionLength == 0) return;

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    rtbMessage.Copy();
                    return;
                }
                catch (ExternalException)
                {
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private async void mnuPaste_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    rtbMessage.Paste();
                    return;
                }
                catch (ExternalException)
                {
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void mnuSelectAll_Click(object sender, EventArgs e)
        {
            try { rtbMessage.SelectAll(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void mnuDelete_Click(object sender, EventArgs e)
        {
            try { if (rtbMessage.SelectionLength > 0) rtbMessage.SelectedText = ""; }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        #endregion " Context Menu "

        #region " File Operations "

        private void LoadMessage()
        {
            try
            {
                if (File.Exists(MessageFilePath))
                {
                    rtbMessage.Text = File.ReadAllText(MessageFilePath);
                }
                else
                {
                    string defaultMessage = "";
                    File.WriteAllText(MessageFilePath, defaultMessage);
                    rtbMessage.Text = defaultMessage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading message: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rtbMessage.Text = "Error loading message content.";
            }
        }

        private void SaveMessage()
        {
            try
            {
                File.WriteAllText(MessageFilePath, rtbMessage.Text);
                MessageBox.Show("Message saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving message: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion " File Operations "

        #region " Helper Functions "

        private string GetWordAtCharIndex(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length) return string.Empty;

            int startPos = index;
            while (startPos > 0 && (char.IsLetterOrDigit(text[startPos - 1]) || text[startPos - 1] == '\'' || text[startPos - 1] == '’'))
            {
                startPos--;
            }

            int endPos = index;
            while (endPos < text.Length - 1 && (char.IsLetterOrDigit(text[endPos + 1]) || text[endPos + 1] == '\'' || text[endPos + 1] == '’'))
            {
                endPos++;
            }

            if (endPos >= startPos)
            {
                return text.Substring(startPos, endPos - startPos + 1);
            }
            return string.Empty;
        }

        private int FindWordStart(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length) return -1;

            int startPos = index;
            while (startPos > 0 && (char.IsLetterOrDigit(text[startPos - 1]) || text[startPos - 1] == '\'' || text[startPos - 1] == '’'))
            {
                startPos--;
            }
            return startPos;
        }

        #endregion " Helper Functions "
    }
}