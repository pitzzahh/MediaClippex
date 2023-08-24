using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class DownloadedVideoCardViewModel : BaseViewModel
{
    [ObservableProperty] private string? _description;

    [ObservableProperty] private string? _duration;
    [ObservableProperty] private string? _fileSize;

    [ObservableProperty] private string? _fileType;

    [ObservableProperty] private string? _imageUrl;

    [ObservableProperty] private string? _path;
    [ObservableProperty] private string? _title;

    public DownloadedVideoCardViewModel(string? title, string? description, string? fileType, string? fileSize,
        string? path, string? duration, string? imageUrl)
    {
        Title = title;
        Description = description;
        FileType = fileType;
        FileSize = fileSize;
        Path = path;
        Duration = duration;
        ImageUrl = imageUrl;
    }

    [RelayCommand]
    private void OpenVideo()
    {
        if (string.IsNullOrEmpty(Path) || !File.Exists(Path)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // Handle any exceptions that might occur
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}