using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Src.Forms.Helper
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MessageBox : Page
    {
        public MessageBox()
        {
            InitializeComponent();
        }
        public static DialogResult Show(string message, string caption = "Info", MessageBoxType type = default) 
            => ShowInternal(new MessageBoxConfig(message, caption));
        public static DialogResult Show(MessageBoxConfig config)
            => ShowInternal(config);
        public static DialogResult Show(
            string message, 
            string caption = "Info",
            MessageBoxTemplate template = MessageBoxTemplate.Simple, 
            MessageBoxType type = MessageBoxType.OKCancel, 
            MessageBoxIcon icon = MessageBoxIcon.Info, 
            ElementTheme theme = ElementTheme.Default)
            => ShowInternal(DefaultConfig(message, caption, template), type, icon);
        public static async Task<DialogResult> ShowAsync(string message, string caption = "Info")
            => await ShowInternalAsync(new MessageBoxConfig(message, caption));
        public static async Task<DialogResult> ShowAsync(MessageBoxConfig config)
            => await ShowInternalAsync(config);
        public static async Task<DialogResult> ShowAsync(
            string message, 
            string caption = "Info",
            MessageBoxTemplate template = MessageBoxTemplate.Simple, 
            MessageBoxType type = MessageBoxType.OKCancel, 
            MessageBoxIcon icon = MessageBoxIcon.Info, 
            ElementTheme theme = ElementTheme.Default)
            => await ShowInternalAsync(DefaultConfig(message, caption, template), type, icon);

        private static DialogResult ShowInternal(
            MessageBoxConfig config, 
            MessageBoxType type = MessageBoxType.OKCancel, 
            MessageBoxIcon icon = MessageBoxIcon.Info, 
            ElementTheme theme = ElementTheme.Default)
        {
            bool isClosed = false;
            var MessageBoxWindow = new MessageBoxWindow();
            MessageBoxWindow.Closed += (_,_)=>isClosed=true;
            var cd = CreateDialog(config);
            var result = cd.ShowAsync().GetResults();
            return isClosed ? DialogResult.Cancel : TransformResult(result, type);
        }
        private static async Task<DialogResult> ShowInternalAsync(
            MessageBoxConfig config, 
            MessageBoxType type = MessageBoxType.OKCancel, 
            MessageBoxIcon icon = MessageBoxIcon.Info, 
            ElementTheme theme = ElementTheme.Default)
        {
            bool isClosed = false;
            var MessageBoxWindow = new MessageBoxWindow();
            MessageBoxWindow.Closed += (_, _) => isClosed = true;
            var cd = CreateDialog(config);
            var result = await cd.ShowAsync();
            return isClosed ? DialogResult.Cancel : TransformResult(result, type);
        }
        private static DialogResult TransformResult (ContentDialogResult result, MessageBoxType type)
            => result switch
        {
            ContentDialogResult.None => type switch
            {
                MessageBoxType.OK => DialogResult.None,
                MessageBoxType.OKCancel => DialogResult.Cancel,
                MessageBoxType.YesNo => DialogResult.None,
                MessageBoxType.YesNoCancel => DialogResult.Cancel,
                _ => DialogResult.None,
            },
            ContentDialogResult.Primary => type switch
            {
                MessageBoxType.OK => DialogResult.OK,
                MessageBoxType.OKCancel => DialogResult.OK,
                MessageBoxType.YesNo => DialogResult.Yes,
                MessageBoxType.YesNoCancel => DialogResult.Yes,
                _ => DialogResult.None,
            },
            ContentDialogResult.Secondary => type switch
            {
                MessageBoxType.YesNo => DialogResult.No,
                MessageBoxType.YesNoCancel => DialogResult.No,
                _ => DialogResult.None,
            },
            _ => DialogResult.None,
        };
        public static MessageBoxConfig DefaultConfig(string message, string caption = "Info", MessageBoxTemplate template = MessageBoxTemplate.Simple) => template switch
        {
            MessageBoxTemplate.Simple   => new MessageBoxConfig(message, TitleBar: caption),
            MessageBoxTemplate.Detailed => new MessageBoxConfig(message, StorageBox.MESSAGE_BOX_WIDTH + 100, StorageBox.MESSAGE_BOX_HEIGHT + 100, StorageBox.MESSAGE_BOX_TITLE,    MessageBoxType.OKCancel,   MessageBoxIcon.Info),
            MessageBoxTemplate.List     => new MessageBoxConfig(message, StorageBox.MESSAGE_BOX_WIDTH + 200, StorageBox.MESSAGE_BOX_HEIGHT + 200, StorageBox.MESSAGE_BOX_TITLE,    MessageBoxType.OKCancel,   MessageBoxIcon.Info),
            MessageBoxTemplate.Warning  => new MessageBoxConfig(message, StorageBox.MESSAGE_BOX_WIDTH,       StorageBox.MESSAGE_BOX_HEIGHT,       "Warning",                  MessageBoxType.OKCancel,   MessageBoxIcon.Warning),
            MessageBoxTemplate.Error    => new MessageBoxConfig(message, StorageBox.MESSAGE_BOX_WIDTH,       StorageBox.MESSAGE_BOX_HEIGHT,       "Error",                    MessageBoxType.OKCancel,   MessageBoxIcon.Error),
            _ => new MessageBoxConfig(message),
        };
        public static ContentDialog CreateDialog(MessageBoxConfig config)
        {

            (string? close, string? prim, string? sec) = GetButtonText(config);
            var dialog = new ContentDialog
            {
                Title = config.TitleBar,
                Content = config.Message,
                RequestedTheme = config.Theme,
                Width = config.Size.Width,
                Height = config.Size.Height,
                CloseButtonText = close,
                PrimaryButtonText = prim,
                SecondaryButtonText = sec,
                DefaultButton = ContentDialogButton.Primary,
            };
            return dialog;
        }
        private static (string? closeText, string? primaryText, string? secondaryText) GetButtonText(MessageBoxConfig config) 
            => config.Type switch
        {
            MessageBoxType.OKCancel => (closeText: config.CancelButtonText, primaryText: config.OkButtonText, secondaryText: null),
            MessageBoxType.YesNo => (closeText: null, primaryText: config.YesButtonText, secondaryText: config.NoButtonText),
            MessageBoxType.YesNoCancel => (closeText: config.CancelButtonText, primaryText: config.YesButtonText, secondaryText: config.NoButtonText),
            _ => (closeText: null, primaryText: config.OkButtonText, secondaryText: null),
        };
        public record MessageBoxConfig
        { 
            public string Message;
            public string TitleBar;
            public string OkButtonText;
            public string CancelButtonText;
            public string YesButtonText;
            public string NoButtonText;
            public (int Width, int Height) Size;
            public MessageBoxType Type;
            public MessageBoxIcon Icon;
            public MessageBoxTemplate Template;
            public ElementTheme Theme;
            public MessageBoxConfig(string Message) : this(Message, Type: MessageBoxType.OKCancel) { }
            public MessageBoxConfig(string Message, string TitleBar) 
                : this(Message, StorageBox.MESSAGE_BOX_WIDTH, StorageBox.MESSAGE_BOX_HEIGHT, TitleBar, MessageBoxType.OKCancel) { }
            public MessageBoxConfig(string Message, int Width = StorageBox.MESSAGE_BOX_WIDTH, int Height = StorageBox.MESSAGE_BOX_HEIGHT, string TitleBar = StorageBox.MESSAGE_BOX_TITLE) 
                : this(Message, Width, Height, TitleBar, MessageBoxType.OKCancel) { }
            public MessageBoxConfig(
                string Message,
                int Width = StorageBox.MESSAGE_BOX_HEIGHT,
                int Height = StorageBox.MESSAGE_BOX_WIDTH,
                string TitleBar = StorageBox.MESSAGE_BOX_TITLE,
                MessageBoxType Type = MessageBoxType.OKCancel,
                MessageBoxIcon Icon = MessageBoxIcon.Info,
                MessageBoxTemplate Template = MessageBoxTemplate.Simple,
                ElementTheme Theme = ElementTheme.Default)
            {
                this.Message = Message;
                this.TitleBar = TitleBar;
                this.Type = Type;
                this.Icon = Icon;
                this.Template = Template;
                this.Size = (Width, Height);
                this.OkButtonText = StorageBox.MESSAGE_BOX_OK;
                this.CancelButtonText = StorageBox.MESSAGE_BOX_CANCEL;
                this.YesButtonText = StorageBox.MESSAGE_BOX_YES;
                this.NoButtonText = StorageBox.MESSAGE_BOX_NO;
            }
        }
        public enum DialogResult
        {
            OK,
            Cancel,
            Yes,
            No,
            None
        }
        public enum MessageBoxType
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel
        }
        public enum MessageBoxIcon
        {
            Info,
            Warning,
            Error,
            Question,
            None
        }
        public enum MessageBoxTemplate
        {
            Simple,
            Detailed,
            List,
            Error,
            Warning
        }
    }
}
