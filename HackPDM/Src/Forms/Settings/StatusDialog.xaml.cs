using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using CommunityToolkit.WinUI.UI.Controls;

using HackPDM.ClientUtils;
using HackPDM.Data;
using HackPDM.Forms.Hack;
using HackPDM.Src.ClientUtils.Types;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Windows.UI.Core;

using Setting = HackPDM.Properties.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Forms.Settings;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StatusDialog : Page
{
    public Window? ParentWindow { get; set; }
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
    public void AddStatusLine(StatusMessage action, string description)
    {
        AddStatusLine((action, description));
    }
    public void AddStatusLines(params (StatusMessage action, string description)[] values)
    {
        AddStatusLinesInternal([.. values]);
    }
    public void AddStatusLines(ConcurrentQueue<(StatusMessage action, string description)> values)
    {
        List<(StatusMessage, string)> batch = new(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            if (values.TryDequeue(out (StatusMessage action, string description) item)) batch.Add(item);
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
        if (!ComeBackWithThreadAccess(() => SetProgressBarInternal(@params))) return;

        int max, value;
        value = @params[0];
        max = @params[1] > value ? @params[1] : value;

        fileCheckStatus.Maximum = max;
        fileCheckStatus.Value = value;
        ProgressText.Text = $"({(value / (float)max) * 100:f2}%)\n{value} / {max}";
        SkippedLabel.Text = $"({HackFileManager.SkipCounter}) Skipped";
    }
    private bool ComeBackWithThreadAccess(DispatcherQueueHandler handler)
    {
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcher.HasThreadAccess)
        {
            return dispatcher.TryEnqueue(handler);
        }
        return false;
    }

    private delegate void AddStatusLinesDel(List<(StatusMessage action, string description)> values);
    private void AddStatusLinesInternal(List<(StatusMessage action, string description)> values)
    {
        if (!ComeBackWithThreadAccess(() => AddStatusLinesInternal(values))) return;

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
        foreach (var (action, description) in values)
        {
			
        }
        MessageLog.EndUpdate();
        
    }
    private void AddMessageToList((StatusMessage action, string description) values)
    {
        DataGridRow lvItem = new()
        {
            DataContext = new BasicStatusMessage { Message = values.description, Status = values.action }
        };
        // set background color, based on status action
        DataGrid? grid = null;
        switch (values.action)
        {
            case StatusMessage.PROCESSING:
            {
                lvItem.Foreground = ColorProcessing; 
                grid = StatusList
                break;
            }
            case StatusMessage.SKIP:
            {
                lvItem.Foreground = ColorSkip; 
                break;
            }
            case StatusMessage.FOUND:
            {
                lvItem.Foreground = ColorFound; 
                break;
            }
            case StatusMessage.SUCCESS:
            {
                lvItem.Foreground = ColorSuccess; 
                break;
            }
            case StatusMessage.WARNING:
            {
                lvItem.Background = ColorWarning; 
                break;
            }
            case StatusMessage.ERROR:
            {
                lvItem.Background = ColorError; 
                _errorCount++; 
                break;
            }
            default: break;
        }

        MessageLog.Items.Add(lvItem);
        MessageLog.EnsureVisible(MessageLog.Items.Count - 1);
    }
    private delegate void AddStatusLineDel((StatusMessage, string) @params);
    private void AddStatusLine((StatusMessage action, string description) @params)
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
                
                MessageLog.Items.Add(lvItem);
                MessageLog.EnsureVisible(MessageLog.Items.Count - 1);

            }
        });
    }
    private void ColorizeStatus((StatusMessage action, string description) values, DataGridRow item)
    {
        switch (values.action)
        {
            case StatusMessage.PROCESSING: item.Foreground = ColorProcessing; break;
            case StatusMessage.SKIP: item.Foreground = ColorSkip; break;
            case StatusMessage.FOUND: item.Foreground = ColorFound; break;
            case StatusMessage.SUCCESS: item.Foreground = ColorSuccess; break;
            case StatusMessage.WARNING: item.Background = ColorWarning; break;
            case StatusMessage.ERROR: item.Background = ColorError; _errorCount++; break;
            default: break;
        }
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
            AddStatusLine(StatusMessage.ERROR, $"Encountered {_errorCount} errors");
        else if (cbxAutoClose.Checked == true)
            this.Close();
        cmdCancel.Enabled = false;
        cmdClose.Enabled = true;
    }

    private void StatusSettings_Click(object sender, EventArgs e)
    {
        new StatusSettings().Show();
    }
}