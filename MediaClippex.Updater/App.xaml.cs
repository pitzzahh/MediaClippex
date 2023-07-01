using System;
using System.Threading;
using System.Windows;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.Updater;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private static Mutex? _mutex;
    public App()
    {
        BuilderServices.BuildWithContainer(BuildContainer.ConfigureServices());
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "MediaClippexUpdaterMutex", out var createdNew);

        if (!createdNew)
        {
            // An instance of the application is already running, bring it to foreground and exit.
            NativeMethods.BringWindowToForeground(NativeMethods.FindWindow("Updater", "MediaClippex Updater"));
            Current.Shutdown();
            return;
        }
        BuilderServices.Resolve<MainWindow>().Show();
        base.OnStartup(e);
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
    
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool BringWindowToForeground(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    }
}