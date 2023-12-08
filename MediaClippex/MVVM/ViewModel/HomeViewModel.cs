using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using MediaClippex.Services;
using MediaClippex.Services.Helpers;
using MediaClippex.Services.Settings.Interfaces;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;
using Russkyc.DependencyInjection.Interfaces;
using YoutubeExplode.Common;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
public partial class HomeViewModel : BaseViewModel
{
    private readonly IServicesContainer _container;

    [ObservableProperty]
    private ObservableCollection<DownloadedContentCardViewModel> _downloadedVideoCardViewModels = new();

    [ObservableProperty] private bool _hasDownloadHistory;
    [ObservableProperty] private bool _hasQueue;
    [ObservableProperty] private string? _imagePreview;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isPlaylist;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private bool _isQuery;
    [ObservableProperty] private bool _isResolved;

    [ObservableProperty] private ObservableCollection<PreviewCardViewModel> _previewCardViewModels = new();
    [ObservableProperty] private string? _progressInfo;

    [ObservableProperty]
    private ObservableCollection<QueuingContentCardViewModel> _queuingContentCardViewModels = new();

    [ObservableProperty] private bool _showPreview;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private string? _url;

    public HomeViewModel(IServicesContainer container)
    {
        _container = container;
    }

    [RelayCommand]
    private async Task Resolve()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            MessageBox.Show("Please enter a URL.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var isYouTubeVideoUrl = StringService.IsYouTubeVideoUrl(Url);
        var isYouTubePlaylistUrl = StringService.IsYouTubePlaylistUrl(Url);

        IsQuery = !isYouTubeVideoUrl || !isYouTubePlaylistUrl;

        if (!IsQuery)
        {
            MessageBox.Show("Please enter a valid YouTube URL.", "Warning", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Url = "";
            return;
        }

        IsPlaylist = isYouTubePlaylistUrl;
        ProgressInfo = "Processing URL...";
        IsProgressIndeterminate = true;
        IsProcessing = true;

        try
        {
            if (IsQuery)
            {
                var settings = _container.Resolve<ISettings>();
                var readOnlyList = await VideoService.GetVideos(Url, settings.QueryResultLimit());
                if (readOnlyList.Count == 0)
                {
                    MessageBox.Show("No videos found", "Cannot resolve", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                PreviewCardViewModels.Clear();
                IsProcessing = false;
                ShowPreview = true;
                IsProgressIndeterminate = false;

                foreach (var video in readOnlyList.Distinct())
                {
                    await Task.Delay(700);
                    PreviewCardViewModels.Insert(0, new PreviewCardViewModel(
                        _container,
                        video.Title,
                        StringService.ConvertToTimeFormat(video.Duration.GetValueOrDefault()),
                        video.Author.ChannelTitle,
                        video.Thumbnails.GetWithHighestResolution().Url,
                        video.Url,
                        true
                    ));
                }
            }
            else if (IsPlaylist)
            {
                var readOnlyList = await VideoService.GetPlaylistVideos(Url);
                var playlistInfo = await VideoService.GetPlaylistInfo(Url);
                var playListTitle = FileUtil.FixFileName(playlistInfo.Title);
                if (readOnlyList.Count == 0)
                {
                    MessageBox.Show("Playlist not found.", "Cannot resolve", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                PreviewCardViewModels.Clear();
                IsProcessing = false;
                ShowPreview = true;
                IsProgressIndeterminate = false;

                foreach (var playlistVideo in readOnlyList.Distinct())
                {
                    await Task.Delay(700);
                    PreviewCardViewModels.Insert(0, new PreviewCardViewModel(
                        _container,
                        playlistVideo.Title,
                        StringService.ConvertToTimeFormat(playlistVideo.Duration.GetValueOrDefault()),
                        playlistVideo.Author.ChannelTitle,
                        playlistVideo.Thumbnails.GetWithHighestResolution().Url,
                        playlistVideo.Url,
                        true,
                        playListTitle
                    ));
                }
            }
            else
            {
                var video = await VideoService.GetVideo(Url);

                IsProcessing = false;
                ShowPreview = true;
                IsProgressIndeterminate = false;

                PreviewCardViewModels.Insert(0, new PreviewCardViewModel(
                    _container,
                    video.Title,
                    StringService.ConvertToTimeFormat(video.Duration.GetValueOrDefault()),
                    video.Author.ChannelTitle,
                    video.Thumbnails.GetWithHighestResolution().Url,
                    Url
                ));
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Something went wrong resolving: {e.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            ShowPreview = false;
            IsProcessing = false;
        }
        finally
        {
            Url = IsQuery ? Url : "";
        }
    }

    public void GetQueuingVideos()
    {
        var task = Task.Run(
            () => _container.Resolve<IUnitOfWork>().QueuingContentRepository.GetAll().Reverse().ToList());

        task.GetAwaiter().OnCompleted(() =>
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                QueuingContentCardViewModels.Clear();
                var queuingContents = task.Result;
                HasQueue = queuingContents.Count > 0;
                if (!HasQueue) return;
                foreach (var video in queuingContents)
                {
                    await Task.Delay(700);
                    QueuingContentCardViewModels.Add(new QueuingContentCardViewModel(
                        _container,
                        video.Title,
                        video.Duration,
                        video.ThumbnailUrl,
                        video.Url,
                        video.SelectedQuality
                    ));
                }
            });
        });
    }

    public void GetDownloadedVideos()
    {
        var task = Task.Run(() => _container.Resolve<IUnitOfWork>().VideosRepository.GetAll().Reverse().ToList());
        task.GetAwaiter().OnCompleted(() =>
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                DownloadedVideoCardViewModels.Clear();
                var videos = task.Result;
                HasDownloadHistory = videos.Count > 0;
                if (!HasDownloadHistory) return;

                foreach (var video in videos)
                {
                    await Task.Delay(700);
                    DownloadedVideoCardViewModels.Add(new DownloadedContentCardViewModel(
                        _container,
                        video.Title,
                        video.FileType,
                        video.FileSize,
                        video.Path,
                        video.Duration,
                        video.ThumbnailUrl
                    ));
                }
            });
        });
    }
}