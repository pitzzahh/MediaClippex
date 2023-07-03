using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.MVVM.ViewModel;

public partial class LauncherViewModel : ObservableObject
{
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _progressBarVisibility = "Collapsed";
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private bool _isProgressIndeterminate;
    
    
}