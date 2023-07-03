using System.Diagnostics;
using System.Net.Mime;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Launcher.MVVM.ViewModel;

public partial class LauncherViewModel : ObservableObject
{
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _progressBarVisibility = "Collapsed";
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private bool _isProgressIndeterminate;

    [RelayCommand]
    private void Download()
    {
        Process.Start("MediaClippex.exe");
        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.Shutdown();
        });
    }
}