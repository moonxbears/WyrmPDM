using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using HackPDM.Src.Extensions.General;
using Setting = HackPDM.Properties.Settings;
using System.Threading.Tasks;
using HackPDM.ClientUtils;
using System.Collections.Concurrent;
using HackPDM.Src.Forms.Hack;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Src.Forms.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StatusDialog : Page
    {
        public static Brush ColorProcessing { get; set; } = StorageBox.BRUSH_DARK_BLUE;
        public static Brush ColorSkip { get; set; } = StorageBox.BRUSH_DARK_GRAY;
        public static Brush ColorFound { get; set; } = StorageBox.BRUSH_DARK_GRAY;
        public static Brush ColorSuccess { get; set; } = StorageBox.BRUSH_DARK_OLIVE_GREEN;
        public static Brush ColorWarning { get; set; } = StorageBox.BRUSH_MUSTARD_YELLOW;
        public static Brush ColorError { get; set; } = StorageBox.BRUSH_DARK_RED;
        public static Brush ColorDefaultFore { get; set; } = StorageBox.BRUSH_BLACK;
        public static Brush ColorDefaultBack { get; set; } = StorageBox.BRUSH_WHITE;

        int ErrorCount = 0;
        public static bool? SkipText
        {
            get
            {
                field ??= Setting.Get("SkipText", field);
                return field;
            }
            set
            {
                field = value;
                Setting.Set("SkipText", field);
            }
        }
        public static int? HistoryLength
        {
			get
			{
				field ??= Setting.Get("HistoryLength", field);
				return field;
			}
			set
			{
				field = value;
				Setting.Set("HistoryLength", field);
			}
		}
        public bool DoubleBuff { get; set; } = true;
        public bool Canceled { get; private set; } = false;
        public bool HasLoaded { get; set; } = false;

        public bool ShowStatusDialog(string TitleText)
        {
            //var dlg = new StatusDialog(TitleText);
            this.Text = TitleText;
            this.ShowDialog();
            return this.Canceled;
        }
        public async Task<bool> ShowWait(string titleText)
        {
            this.Text = titleText;
            this.Show();
            return await AsyncHelper.WaitUntil(() => this.Visible, 100, 10000);
        }

        public StatusDialog()
        {
            HackFileManager.QueueAsyncStatus = new();
            InitializeComponent();
            ClearStatus();
            this.Load += new EventHandler(FormLoaded);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Canceled = true;
            base.OnFormClosing(e);
        }

        private StatusDialog(string TitleText) : this()
        {
            HackFileManager.QueueAsyncStatus = new();
            this.Text = TitleText;
            ClearStatus();
        }
        private void FormLoaded(object sender, EventArgs e)
        {
            HasLoaded = true;
        }
        public void ClearStatus()
        {
            
        }
        public void AddStatusLine(string Action, string Description)
        {
            string[] strStatusParams = [Action, Description];
            AddStatusLine(strStatusParams);
        }
        public void AddStatusLines(List<string[]> values)
        {
            AddStatusLinesInternal(values);
        }
        public void AddStatusLines(ConcurrentQueue<string[]> values)
        {
            List<string[]> batch = new(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                if (values.TryDequeue(out string[] item)) batch.Add(item);
                else break;
            }
            AddStatusLinesInternal(batch);
        }

        public void SetProgressBar(int value, int max)
        {
            SetProgressBarInternal([value, max]);
        }
        private delegate void SetProgressBarDel(int[] Params);
        private void SetProgressBarInternal(int[] Params)
        {
            if (this.InvokeRequired)
            {
                SetProgressBarDel del = new(SetProgressBarInternal);
                this.Invoke(del, (object)Params);
            }
            else
            {
                int max, value;
                value = Params[0];
                max = Params[1] > value ? Params[1] : value;

                fileCheckStatus.Maximum = max;
                fileCheckStatus.Value = value;
                ProgressText.Text = $"({(value / (float)max) * 100:f2}%)\n{value} / {max}";
                SkippedLabel.Text = $"({HackFileManager.SkipCounter}) Skipped";
            }
        }

        private delegate void AddStatusLinesDel(List<string[]> values);
        private void AddStatusLinesInternal(List<string[]> values)
        {
            if (this.InvokeRequired)
            {
                AddStatusLinesDel del = new(AddStatusLinesInternal);
                this.Invoke(del, values);
            }
            else
            {
                MessageLog.BeginUpdate();
                int totalCount = MessageLog.Items.Count + values.Count;

                if (totalCount > HistoryLength)
                {
                    // 65 lvM count
                    // 100 values count
                    // 165 total 
                    // 150 history length 
                    // 15 = total - history length
                    // lvM - value = 150
                    int histOffset = totalCount - HistoryLength;
                    for (int i = 0; i < histOffset; i++)
                    {
                        if (MessageLog.Items.Count > 0)
                        {
                            MessageLog.Items.RemoveAt(0);
                        }
                    }
                }
                foreach (var item in values)
                {
                    ListViewItem lvItem = new(item);
                    lvItem.Foreground
                    // set background color, based on status action
                    switch (item[0])
                    {
                        case "PROCESSING": lvItem.Foreground = ColorProcessing; break;
                        case "SKIP": lvItem.Foreground = ColorSkip; break;
                        case "FOUND": lvItem.Foreground = ColorFound; break;
                        case "SUCCESS": lvItem.Foreground = ColorSuccess; break;
                        case "WARNING": lvItem.Background = ColorWarning; break;
                        case "ERROR": lvItem.Background = ColorError; ErrorCount++; break;
                        default: break;
                    }

                    MessageLog.Items.Add(lvItem);
                    MessageLog.EnsureVisible(MessageLog.Items.Count - 1);
                }
                MessageLog.EndUpdate();
            }
        }
        private delegate void AddStatusLineDel(string[] Params);
        private void AddStatusLine(string[] Params)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (this.InvokeRequired)
                {

                    // this is a worker thread so delegate the task to the UI thread
                    AddStatusLineDel del = new(AddStatusLine);
                    this.Invoke(del, (object)Params);

                }
                else
                {

                    // we are executing in the UI thread
                    ListViewItem lvItem = new(Params);

                    // set background color, based on status action
                    switch (Params[0])
                    {
                        case "PROCESSING": lvItem.ForeColor = ColorProcessing; break;
                        case "SKIP": lvItem.ForeColor = ColorSkip; break;
                        case "FOUND": lvItem.ForeColor = ColorFound; break;
                        case "SUCCESS": lvItem.ForeColor = ColorSuccess; break;
                        case "WARNING": lvItem.BackColor = ColorWarning; break;
                        case "ERROR": lvItem.BackColor = ColorError; ErrorCount++; break;
                        default: break;
                    }
                    MessageLog.Items.Add(lvItem);
                    MessageLog.EnsureVisible(MessageLog.Items.Count - 1);

                }
            });
        }

        private void CmdCancelClick(object sender, EventArgs e)
        {
            Canceled = true;
            this.Close();
        }

        void CmdCloseClick(object sender, EventArgs e)
        {
            this.Close();
        }

        public void OperationCompleted()
        {
            if (ErrorCount != 0)
                AddStatusLine("ERROR", String.Format("Encountered {0} errors", ErrorCount));
            else if (cbxAutoClose.Checked == true)
                this.Close();
            cmdCancel.Enabled = false;
            cmdClose.Enabled = true;
        }

        private void StatusSettings_Click(object sender, EventArgs e)
        {
            new StatusSettings().Show();
        }

        private void MessageLog_DrawItem(object sender, DrawListViewItemEventArgs e)
        {

        }

        private void MessageLog_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (MessageLog.Items.Count > HistoryLength)
            {
                MessageLog.Items.RemoveAt(0);
            }
        }
    }
}
