﻿using System.IO;

using HackPDM.Src.ClientUtils.Types;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using ProfileManager = HackPDM.Forms.Settings.ProfileManager;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class HackApp : Application
{
    public static Window? Window;
    public static Frame? RootFrame;
    // To fix CS0121, fully qualify the InitializeComponent() call to specify the correct method.
    // If your project has both a generated partial method and a user-defined method, use the global:: prefix.

    public HackApp()
    {
        InitializeComponent();
        Setup();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        var rootFrame = new Frame();
        Window.Activate();
        Window.Content = rootFrame;
        rootFrame.Navigate(typeof(ProfileManager));
    }
    private void Setup ()
    {
        string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), StorageBox.APP_NAME);
        if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);

        Notifier.FileCheckLoop(); // start file check loop in background
    }
}