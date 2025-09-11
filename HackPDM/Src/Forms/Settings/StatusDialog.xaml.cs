using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using HackPDM.ClientUtils;
using HackPDM.Forms.Hack;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Setting = HackPDM.Properties.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Forms.Settings;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StatusDialog : Page
{
    public static Brush ColorProcessing { get; set; } = StorageBox.BrushDarkBlue;
    public static Brush ColorSkip { get; set; } = StorageBox.BrushDarkGray;
    public static Brush ColorFound { get; set; } = StorageBox.BrushDarkGray;
    public static Brush ColorSuccess { get; set; } = StorageBox.BrushDarkOliveGreen;
    public static Brush ColorWarning { get; set; } = StorageBox.BrushMustardYellow;
    public static Brush ColorError { get; set; } = StorageBox.BrushDarkRed;
    public static Brush ColorDefaultFore { get; set; } = StorageBox.BrushBlack;
    public static Brush ColorDefaultBack { get; set; } = StorageBox.BrushWhite;

    int _errorCount = 0;
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

    public bool ShowStatusDialog(string titleText)
    {
        //var dlg = new StatusDialog(TitleText);
        this.Text = titleText;
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

    private StatusDialog(string titleText) : this()
    {
        HackFileManager.QueueAsyncStatus = new();
        this.Text = titleText;
        ClearStatus();
    }
    private void FormLoaded(object sender, EventArgs e)
    {
        HasLoaded = true;
    }
    public void ClearStatus()
    {
            
    }
    public void AddStatusLine(string action, string description)
    {
        string[] strStatusParams = [action, description];
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
    private delegate void SetProgressBarDel(int[] @params);
    private void SetProgressBarInternal(int[] @params)
    {
        if (this.InvokeRequired)
        {
            SetProgressBarDel del = new(SetProgressBarInternal);
            this.Invoke(del, (object)@params);
        }
        else
        {
            int max, value;
            value = @params[0];
            max = @params[1] > value ? @params[1] : value;

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
                    case "ERROR": lvItem.Background = ColorError; _errorCount++; break;
                    default: break;
                }

                MessageLog.Items.Add(lvItem);
                MessageLog.EnsureVisible(MessageLog.Items.Count - 1);
            }
            MessageLog.EndUpdate();
        }
    }
    private delegate void AddStatusLineDel(string[] @params);
    private void AddStatusLine(string[] @params)
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            if (this.InvokeRequired)
            {

                // this is a worker thread so delegate the task to the UI thread
                AddStatusLineDel del = new(AddStatusLine);
                this.Invoke(del, (object)@params);

            }
            else
            {

                // we are executing in the UI thread
                ListViewItem lvItem = new(@params);

                // set background color, based on status action
                switch (@params[0])
                {
                    case "PROCESSING": lvItem.ForeColor = ColorProcessing; break;
                    case "SKIP": lvItem.ForeColor = ColorSkip; break;
                    case "FOUND": lvItem.ForeColor = ColorFound; break;
                    case "SUCCESS": lvItem.ForeColor = ColorSuccess; break;
                    case "WARNING": lvItem.BackColor = ColorWarning; break;
                    case "ERROR": lvItem.BackColor = ColorError; _errorCount++; break;
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
        if (_errorCount != 0)
            AddStatusLine("ERROR", String.Format("Encountered {0} errors", _errorCount));
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