using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using Russkyc.DependencyInjection.Implementations;

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
        UnitOfWork = BuilderServices.Resolve<IUnitOfWork>();
    }

    private IUnitOfWork UnitOfWork { get; }

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

    [RelayCommand]
    private async Task DeleteVideo()
    {
        if (string.IsNullOrEmpty(Path) || !File.Exists(Path)) return;
        try
        {
            var foundDownloadedVideo = UnitOfWork.VideosRepository
                .Find(v => v.Title != null && v.Title.Equals(Title))
                .FirstOrDefault();

            if (foundDownloadedVideo == null) return;

            UnitOfWork.VideosRepository
                .Remove(foundDownloadedVideo);
            if (UnitOfWork.Complete() == 0) return;
            File.Delete(Path);
            await Task.Run(BuilderServices.Resolve<MediaClippexViewModel>().GetDownloadedVideos);
        }
        catch (Exception)
        {
            // Handle any exceptions that might occur
        }
    }
}