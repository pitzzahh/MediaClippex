using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class DownloadedVideoCardViewModel : BaseViewModel
{
    [ObservableProperty] private string? _title;

    [ObservableProperty] private string? _description;
    
    [ObservableProperty] private string? _fileSize;
    
    [ObservableProperty] private string? _path;

    [ObservableProperty] private string? _duration;

    [ObservableProperty] private string? _imageUrl;

    public DownloadedVideoCardViewModel(string? title, string? description, string? fileSize, string? path, string? duration, string? imageUrl)
    {
        Title = title;
        Description = description;
        FileSize = fileSize;
        Path = path;
        Duration = duration;
        ImageUrl = imageUrl;
    }

    [RelayCommand]
    private void OpenVideo()
    {
        if (!string.IsNullOrEmpty(Path) && File.Exists(Path))
        {
            Process.Start(Path);
        }
    }

}