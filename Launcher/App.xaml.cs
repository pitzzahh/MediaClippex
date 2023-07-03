using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Launcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe")))
        {
            Process.Start(Path.Combine(AppContext.BaseDirectory, "MediaClippex.exe"));
            Current.Shutdown();
        }
        else base.OnStartup(e);
    }
}