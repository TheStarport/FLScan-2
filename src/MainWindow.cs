using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

using FLScanIE.Logging;
using FLScanIE.Util_Functions;

namespace FLScanIE
{
    // TODO: Interface, Missions, RandomMissions
    public partial class MainWindow : Form
    {
        private string[] filterExcludes;
        private int selectedFilter = -1;
        private Dictionary<string, Type> logFilterTypes = new Dictionary<string, Type>();

        /// <summary>
        /// The background processing thread. We do the processing in the background
        /// 'cause it's cool to do it.
        /// </summary>
        private BackgroundWorker bgWkr = null;

        public MainWindow()
        {
            InitializeComponent();
            updateUI = new UpdateUIAddLogDelegate(UpdateUIAddLog);
            Logger.HandleLog += HandleLog;
            checkDisableUTF.Checked = Checker.DisableUTF = Properties.Settings.Default.setDisableUTF;
            InitFilter();
        }

        private void InitFilter()
        {
            logFilterTypes = new Dictionary<string, Type>();
            logFilterTypes.Add("Setting not found", typeof(SettingNotFoundLogEntry));
            logFilterTypes.Add("File not found", typeof(FileNotFoundLogEntry));
            logFilterTypes.Add("Hardpoint not found", typeof(HardpointNotFoundLogEntry));
            logFilterTypes.Add("ID not found", typeof(IDNotFoundLogEntry));
            logFilterTypes.Add("Wrong Value-Count", typeof(InvalidValueCountLogEntry));
            logFilterTypes.Add("Invalid Value", typeof(InvalidValueLogEntry));
            logFilterTypes.Add("Doubled Value", typeof(DublicateValueLogEntry));

            checkedListBoxFilter.Items.Clear();
            checkedListBoxFilter.Items.AddRange(logFilterTypes.Select(t => t.Key).ToArray());

            checkedListBoxLogLevel.Items.Clear();
            checkedListBoxLogLevel.Items.AddRange(Enum.GetNames(typeof(LogLevel)));

            checkedListBoxChecks.Items.Clear();
            checkedListBoxChecks.Items.AddRange(Enum.GetNames(typeof(Checks)).Where(s => s != "None").ToArray());

            LoadCheckedListbox(Properties.Settings.Default.setCheckedFilters, checkedListBoxFilter);
            LoadCheckedListbox(Properties.Settings.Default.setCheckedLevels, checkedListBoxLogLevel);
            LoadCheckedListbox(Properties.Settings.Default.setCheckedChecks, checkedListBoxChecks);

            try
            {
                filterExcludes = Properties.Settings.Default.setFilters.Select(s => s.Replace("\\n", "\r\n")).ToArray();
            }
            catch { }
            if (filterExcludes == null)
            {
                filterExcludes = new string[checkedListBoxFilter.Items.Count];
                for (int i = 0; i < filterExcludes.Length; i++)
                {
                    filterExcludes[i] = string.Empty;
                }
            }
            checkedListBoxFilter.SelectedIndex = 0;
        }

        private static void LoadCheckedListbox(int[] val, CheckedListBox listbox)
        {
            if (val == null)
            {
                for (int i = 0; i < listbox.Items.Count; i++)
                {
                    listbox.SetItemChecked(i, true);
                }
                return;
            }
            foreach (var checkedFilter in val)
            {
                if (checkedFilter >= 0 && checkedFilter < listbox.Items.Count)
                    listbox.SetItemChecked(checkedFilter, true);
            }
        }

        private void SaveFilter()
        {
            Properties.Settings.Default.setCheckedFilters = checkedListBoxFilter.CheckedIndices.Cast<int>().ToArray();
            Properties.Settings.Default.setCheckedLevels = checkedListBoxLogLevel.CheckedIndices.Cast<int>().ToArray();
            Properties.Settings.Default.setCheckedChecks = checkedListBoxChecks.CheckedIndices.Cast<int>().ToArray();
            filterExcludes[checkedListBoxFilter.SelectedIndex] = textBoxFilterExclude.Text;
            Properties.Settings.Default.setFilters = filterExcludes.Select(s => s.Replace("\r\n", "\\n")).ToArray();
        }

        private void HandleLog(LogEntry log)
        {
            ShowLogEntry(log);
        }

        private void ShowLogEntry(LogEntry log)
        {
            if(!checkedListBoxLogLevel.CheckedItems.Cast<object>().Select(c => c.ToString().ToLower()).Contains(log.Loglevel.ToString().ToLower()))
                return;

            if(log.GetType().Name != typeof(LogEntry).Name)
            {
                var e = logFilterTypes.First(f => f.Value.Name == log.GetType().Name);
                var list = checkedListBoxFilter.CheckedItems.Cast<string>();
                var filterExcludes = this.filterExcludes.Select(ee => ee.Replace("\r", "").ToLower()).ToArray();
                var str = log.ToString().ToLower();

                if(!list.Contains(e.Key))
                    return;
                if (filterExcludes.Any(ee => ee.Split('\n').Any(eee => eee.Length != 0 && str.Contains(eee))))
                    return;
            }

            string logStr = log.ToString();

            switch (log.Loglevel)
            {
                case LogLevel.info:
                    AddLog(logStr, Color.DarkGray);
                    break;
                case LogLevel.warning:
                    AddLog(logStr, Color.Orange);
                    break;
                case LogLevel.error:
                    AddLog(logStr, Color.Red);
                    break;
                case LogLevel.fatal:
                    AddLog(logStr, Color.Red);
                    break;
            }
        }

        private void buttonScan_Click(object sender, EventArgs e)
        {
            if (bgWkr == null)
            {
                bgWkr = new BackgroundWorker();
                richTextBox.Clear();
                buttonScan.Enabled = false;
                buttonScan.Text = "Scanning...";
                bgWkr.DoWork += new DoWorkEventHandler(ScanIt);
                bgWkr.WorkerReportsProgress = true;
                bgWkr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ScanItCompleted);
                bgWkr.WorkerSupportsCancellation = true;
                bgWkr.RunWorkerAsync();
            }
            else
            {
                bgWkr.CancelAsync();
            }
        }

        /// <summary>
        /// Scan and check everything!
        /// </summary>
        private void ScanIt(object sender, DoWorkEventArgs e)
        {
            Logger.ILog("Scan started");

            Checker.Checks = GetChecksEnum();
            bool retn = Checker.Parse(Properties.Settings.Default.setFlDir);
            if (!retn)
                return;
            Checker.Check();

            Logger.ILog("Scan complete");
        }

        private Checks GetChecksEnum()
        {
            Checks tmp = Checks.None;
            foreach (var check in checkedListBoxChecks.CheckedItems)
            {
                tmp = tmp | (Checks)Enum.Parse(typeof (Checks), check.ToString());
            }
            return tmp;
        }

        /// <summary>
        /// Called when ScanIt background processing thread completes.
        /// </summary>
        private void ScanItCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            buttonScan.Enabled = true;
            buttonScan.Text = "Scan";
            bgWkr = null;
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveFilter();
            Properties.Settings.Default.Save();
            Logger.Abort(true);
        }

        /// <summary>
        /// Add an error entry to the log file.
        /// </summary>
        /// <param name="details">A human readable description.</param>
        /// <param name="accountID">If the log entry is related to a file operation 
        /// then this parameter contains a path to the directory containing the file.</param>
        public void AddLog(string details, Color color)
        {
            if (InvokeRequired)
            {
                this.Invoke(updateUI, new object[] { details, color });
            }
            else
            {
                UpdateUIAddLog(details, color);
            }
        }

        /// <summary>
        /// A delegate that always runs in the UI thread. This updates the database
        /// which in turn updates the log table.
        /// </summary>
        delegate void UpdateUIAddLogDelegate(string details, Color color);
        UpdateUIAddLogDelegate updateUI;
        protected void UpdateUIAddLog(string text, Color color)
        {
            int oldLength = richTextBox.TextLength;
            richTextBox.AppendText(text + "\r\n");
            
            if (color != Color.Black)
            {
                int oldStart = richTextBox.SelectionStart;
                int oldLen = richTextBox.SelectionLength;

                richTextBox.Select(oldLength, text.Length);
                richTextBox.SelectionColor = color;
                richTextBox.SelectionLength = 0;

                richTextBox.SelectionStart = oldStart;
                richTextBox.SelectionLength = oldLen;
            }
        }


        private void buttonAbout_Click(object sender, EventArgs e)
        {
            richTextBox.Text = Properties.Resources.Readme;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            textBoxFlPath.Text = Properties.Settings.Default.setFlDir;
        }

        private void buttonBrowseFLPath_Click(object sender, EventArgs e)
        {
            var res = folderBrowserDialog.ShowDialog(this);

            if (res != DialogResult.OK)
                return;

            while(!Directory.Exists(folderBrowserDialog.SelectedPath + Path.DirectorySeparatorChar + "EXE"))
            {
                var msgRes = MessageBox.Show(this, "The selected folder doesn't contain an EXE-folder, FLScan won't be able to scan it.\nDo you really want to select this one?", "Error", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (msgRes == DialogResult.No)
                {
                    res = folderBrowserDialog.ShowDialog(this);

                    if (res != DialogResult.OK)
                        return;
                }
                else if (msgRes == DialogResult.Cancel)
                    return;
                else
                    break;
            }

            textBoxFlPath.Text = folderBrowserDialog.SelectedPath;
        }

        private void textBoxFlPath_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.setFlDir = textBoxFlPath.Text;
        }

        private void checkedListBoxFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(selectedFilter != -1)
                filterExcludes[selectedFilter] = textBoxFilterExclude.Text;
            textBoxFilterExclude.Text = filterExcludes[checkedListBoxFilter.SelectedIndex];
            selectedFilter = checkedListBoxFilter.SelectedIndex;
        }

        private void checkDisableUTF_CheckedChanged(object sender, EventArgs e)
        {
            Checker.DisableUTF = checkDisableUTF.Checked;
        }
    }
}
