﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using MediaClippex.MVVM.Model;
using MediaClippex.Services;
using MediaClippex.Services.Helpers;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Interfaces;
using YoutubeExplode.Videos.Streams;

namespace MediaClippex.MVVM.ViewModel;

[Service]
public partial class QueuingContentCardViewModel : BaseViewModel
{
    private readonly IServicesContainer _container;
    private readonly bool _isPartOfPlaylist;
    private readonly string _playListTitle;
    private readonly string _selectedQuality;
    private readonly string _url;
    public readonly CancellationTokenSource CancellationTokenSource = new();
    [ObservableProperty] private bool _canCancelDownload = true;
    [ObservableProperty] private string _duration;
    [ObservableProperty] private string _fileType;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _paused;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private string _thumbnailUrl;
    [ObservableProperty] private string _title;

    public QueuingContentCardViewModel(IServicesContainer container, string title, string duration, string thumbnailUrl,
        string url,
        string selectedQuality, bool isAudio = false, bool isPartOfPlaylist = false, string playListTitle = "")
    {
        _container = container;
        Title = title;
        Duration = duration;
        ThumbnailUrl = thumbnailUrl;
        _url = url;
        _selectedQuality = selectedQuality;
        _isPartOfPlaylist = isPartOfPlaylist;
        _playListTitle = playListTitle;
        IsProcessing = true;
        FileType = isAudio ? "Audio" : "Video";
        UnitOfWork = _container.Resolve<IUnitOfWork>();
        Task.Run(() => DownloadProcess(isAudio));
    }

    private IUnitOfWork UnitOfWork { get; }

    [RelayCommand]
    private void CancelDownload()
    {
        var messageBoxResult = MessageBox.Show("Do you want to cancel the download?", "Cancel Download",
            MessageBoxButton.YesNo);
        if (messageBoxResult != MessageBoxResult.Yes) return;
        _container.Resolve<StorageService>().RemoveFromQueue(this);
        CancellationTokenSource.Cancel();
    }

    private async Task DownloadProcess(bool isAudio)
    {
        var fixedFileName = $"{FileUtil.FixFileName(Title)}";
        var directoryHelper = _container.Resolve<DirectoryService>();
        var videoFilePath = _isPartOfPlaylist
            ? Path.Combine(directoryHelper.GetPlaylistSavingDirectory(_playListTitle), fixedFileName)
            : Path.Combine(directoryHelper.GetVideoSavingDirectory(), fixedFileName);
        var audioFilePath = _isPartOfPlaylist
            ? Path.Combine(directoryHelper.GetPlaylistSavingDirectory(_playListTitle), fixedFileName)
            : Path.Combine(directoryHelper.GetAudioSavingDirectory(), fixedFileName);

        IsProcessing = true;
        var progressHandler = new Progress<double>(p =>
        {
            Progress = p * 100;
            ProgressInfo = $"{Progress:0.00}%";
            if (Progress <= 85) return;
            CanCancelDownload = false;
        });

        var storageService = _container.Resolve<StorageService>();

        try
        {
            // Starting of download
            var streamManifest = await VideoService.GetVideoManifest(_url);

            var savedPath = await InternalDownloadProcess(isAudio, streamManifest, audioFilePath, progressHandler,
                videoFilePath);

            ProgressInfo = "Done";

            var entity = new Video(
                ThumbnailUrl,
                Title,
                Duration,
                FileType,
                savedPath,
                savedPath is null
                    ? "Cannot be determined"
                    : StringService.ConvertBytesToFormattedString(new FileInfo(savedPath).Length)
            );
            UnitOfWork.VideosRepository.Add(entity);
            UnitOfWork.Complete();
            UnitOfWork.Dispose();
            storageService.AddToDownloadHistory(new DownloadedContentCardViewModel(
                _container,
                entity.Title,
                entity.FileType,
                entity.FileSize,
                entity.Path,
                entity.Duration,
                entity.ThumbnailUrl
            ));
        }
        finally
        {
            storageService.RemoveFromQueue(this);
            UnitOfWork.Dispose();
        }
    }

    private async Task<string?> InternalDownloadProcess(bool isAudio, StreamManifest streamManifest,
        string audioFilePath,
        Progress<double> progressHandler, string videoFilePath)
    {
        string? savedPath;
        if (isAudio)
        {
            var audioStreamInfo = VideoService.GetAudioOnlyStream(streamManifest, _selectedQuality);

            await VideoService.DownloadAudioOnly(audioStreamInfo, audioFilePath, progressHandler,
                CancellationTokenSource.Token);
            savedPath = $"{audioFilePath}.mp3";
        }
        else
        {
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var videoStreamInfo = VideoService.GetVideoOnlyStreamInfo(streamManifest, _selectedQuality);
            await VideoService.DownloadMuxed(audioStreamInfo, videoStreamInfo, videoFilePath, progressHandler,
                CancellationTokenSource.Token);
            savedPath = $"{videoFilePath}.{videoStreamInfo.Container}";
        }

        return savedPath;
    }
}