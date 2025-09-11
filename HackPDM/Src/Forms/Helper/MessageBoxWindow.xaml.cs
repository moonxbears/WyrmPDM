using HackPDM.Helper;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Forms.Helper;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MessageBoxWindow : Window
{
    public MessageBoxWindow()
    {
        InitializeComponent();
        WindowHelper.ResizeWindow(this, StorageBox.MESSAGE_BOX_WIDTH, StorageBox.MESSAGE_BOX_HEIGHT);
    }
}