using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using MediaClippex.MVVM.Model;
using MediaClippex.Services;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;
using Russkyc.DependencyInjection.Interfaces;
using YoutubeExplode.Videos.Streams;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton, Registration.AsInterfaces)]
public partial class PreviewCardViewModel : BaseViewModel
{
    private readonly List<string> _audioQualities = new();
    private readonly IServicesContainer _container;
    private readonly string? _url;
    private readonly List<string> _videoQualities = new();
    [ObservableProperty] private string? _author;
    [ObservableProperty] private string _downloadButtonContent = "Download";
    [ObservableProperty] private string? _duration;
    private bool _isAudioOnly;
    [ObservableProperty] private bool _isResolved;

    [ObservableProperty] private ObservableCollection<string> _qualities = new();

    [ObservableProperty] private string? _quality = "Quality";
    private string _selectedQuality = string.Empty;
    [ObservableProperty] private string? _thumbnailUrl;
    [ObservableProperty] private string? _title;

    public PreviewCardViewModel(IServicesContainer container, string? title, string? duration, string? author,
        string? thumbnailUrl, string? url)
    {
        _container = container;
        _title = title;
        _duration = duration;
        _author = author;
        _thumbnailUrl = thumbnailUrl;
        _url = url;
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (_url == null) return;
            var manifest = await VideoService.GetVideoManifest(_url);
            IsResolved = true;
            InitializeVideoResolutions(manifest);
            InitializeAudioResolutions(manifest);
            GetDownloadSize();
            OnPropertyChanged();
        });
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public string SelectedQuality
    {
        get => _selectedQuality;
        set
        {
            _selectedQuality = value;
            GetDownloadSize();
            OnPropertyChanged();
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsAudioOnly
    {
        get => _isAudioOnly;
        set
        {
            _isAudioOnly = value;
            Qualities.Clear();
            if (_isAudioOnly)
                _audioQualities.ForEach(q => Qualities.Add(q));
            else
                _videoQualities.ForEach(q => Qualities.Add(q));
            SelectedQuality = Qualities.First();
            OnPropertyChanged();
        }
    }

    private void GetDownloadSize()
    {
        Task.Run(GetFileSize);
    }

    private async Task GetFileSize()
    {
        if (string.IsNullOrEmpty(_url)) return;
        var manifest = await VideoService.GetVideoManifest(_url);
        var video = await VideoService.GetVideo(_url);
        if (!video.Duration.HasValue) return;
        DownloadButtonContent = IsAudioOnly
            ? $"Download [{VideoService.GetAudioFileSizeFormatted(manifest, video, SelectedQuality)}]"
            : $"Download [{VideoService.GetVideoFileSizeFormatted(manifest, video, SelectedQuality)}]";
    }

    private void InitializeVideoResolutions(StreamManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(_url)) return;
        _videoQualities.Clear();
        Qualities.Clear();
        manifest.GetVideoOnlyStreams()
            .Select(s => s.VideoQuality.Label)
            .Distinct()
            .ToList()
            .ForEach(s => _videoQualities.Add(s));
        _videoQualities.ForEach(q => Qualities.Add(q));
        SelectedQuality = Qualities.First();
    }

    private void InitializeAudioResolutions(StreamManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(_url)) return;
        _audioQualities.Clear();
        manifest.GetAudioOnlyStreams()
            .Select(a => a.Bitrate.ToString())
            .Distinct()
            .ToList()
            .ForEach(s => _audioQualities.Add(s));
    }

    [RelayCommand]
    private void Remove()
    {
        var mediaClippexViewModel = _container.Resolve<HomeViewModel>();
        mediaClippexViewModel.HasQueue = mediaClippexViewModel.QueuingContentCardViewModels.Count > 0;
        mediaClippexViewModel.PreviewCardViewModels.Remove(this);
        mediaClippexViewModel.ShowPreview = mediaClippexViewModel.PreviewCardViewModels.Count != 0;
    }

    [RelayCommand]
    private void Download()
    {
        if (string.IsNullOrWhiteSpace(SelectedQuality))
        {
            MessageBox.Show("Please select a quality.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_url)) return;

        try
        {
            var mediaClippexViewModel = _container.Resolve<HomeViewModel>();
            mediaClippexViewModel.PreviewCardViewModels.Remove(this);
            mediaClippexViewModel.ShowPreview = mediaClippexViewModel.PreviewCardViewModels.Count > 0;
            mediaClippexViewModel.HasQueue = true;
            mediaClippexViewModel.QueuingContentCardViewModels.Add(new QueuingContentCardViewModel(
                _container,
                Title ?? "404 Title Not Found",
                Duration ?? "00:00:00",
                ThumbnailUrl ??
                "https://media.istockphoto.com/id/1147544806/vector/no-thumbnail-image-vector-graphic.jpg?s=170667a&w=0&k=20&c=-r15fTq303g-Do1h-F1jLdxddwkg4ZTtkdQK1XP2sFk=",
                _url,
                SelectedQuality,
                IsAudioOnly
            ));

            // Adding to QueuingContent Table
            var unitOfWork = _container.Resolve<IUnitOfWork>();
            unitOfWork.QueuingContentRepository.Add(
                new QueuingContent(
                    Title ?? "404 Title Not Found",
                    Duration ?? "00:00:00",
                    ThumbnailUrl ??
                    "https://media.istockphoto.com/id/1147544806/vector/no-thumbnail-image-vector-graphic.jpg?s=170667a&w=0&k=20&c=-r15fTq303g-Do1h-F1jLdxddwkg4ZTtkdQK1XP2sFk=",
                    IsAudioOnly ? "Audio" : "Video",
                    _url,
                    0,
                    "Just Started",
                    SelectedQuality,
                    false,
                    IsAudioOnly)
            );
            var addedToQueueDb = unitOfWork.Complete();
            if (addedToQueueDb == 0)
                MessageBox.Show("Cannot add to db", "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Something went wrong downloading: {e.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsAudioOnly = false;
            IsResolved = false;
        }
    }
}