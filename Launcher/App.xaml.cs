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
        if (File.Exists("ffmpeg.exe"))
        {
            Process.Start("MediaClippex.exe");
            Current.Shutdown();
        }
        else base.OnStartup(e);
    }
}