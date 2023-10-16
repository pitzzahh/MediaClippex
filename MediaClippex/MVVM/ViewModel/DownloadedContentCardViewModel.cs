using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
public partial class DownloadedContentCardViewModel : BaseViewModel
{
    private readonly IServicesContainer _container;
    [ObservableProperty] private string? _duration;
    [ObservableProperty] private string? _fileSize;

    [ObservableProperty] private string? _fileType;

    [ObservableProperty] private string? _imageUrl;

    [ObservableProperty] private string? _path;
    [ObservableProperty] private string? _title;

    public DownloadedContentCardViewModel(IServicesContainer container, string? title, string? fileType,
        string? fileSize,
        string? path, string? duration, string? imageUrl)
    {
        _container = container;
        Title = title;
        FileType = fileType;
        FileSize = fileSize;
        Path = path;
        Duration = duration;
        ImageUrl = imageUrl;
    }

    [RelayCommand]
    private void OpenVideo()
    {
        if (string.IsNullOrEmpty(Path) || !File.Exists(Path))
        {
            MessageBox.Show("File has been deleted or moved to a new directory.", "Cannot find file",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private Task DeleteVideo()
    {
        var messageBoxResult = MessageBox.Show($"Are you sure you want to delete \"{Title}\"?", $"Delete {FileType}",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (messageBoxResult != MessageBoxResult.Yes) return Task.CompletedTask;

        try
        {
            var unitOfWork = _container.Resolve<IUnitOfWork>();
            var foundDownloadedVideo = unitOfWork.VideosRepository
                .Find(v => v.Title != null && v.Title.Equals(Title))
                .FirstOrDefault();

            if (foundDownloadedVideo == null) return Task.CompletedTask;

            unitOfWork.VideosRepository
                .Remove(foundDownloadedVideo);
            if (unitOfWork.Complete() == 0) return Task.CompletedTask;
            if (string.IsNullOrEmpty(Path) && File.Exists(Path)) File.Delete(Path);
            _container.Resolve<HomeViewModel>().DownloadedVideoCardViewModels.Remove(this);
        }
        catch (Exception)
        {
            // Handle any exceptions that might occur
        }

        return Task.CompletedTask;
    }
}