using System.Linq;
using System.Windows;
using MediaClippex.DB.Core;
using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class StorageService
{
    private readonly IUnitOfWork _unitOfWork;

    public StorageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public void RemoveFromQueue(string title)
    {
        MessageBox.Show($"Removing queuing video: {title}");
        var mediaClippexViewModel = BuilderServices.Resolve<MediaClippexViewModel>();

        var queuingContentCardViewModels = mediaClippexViewModel
            .QueuingContentCardViewModels;

        foreach (var contentCardViewModel in queuingContentCardViewModels)
            MessageBox.Show($"Queuing View Model: {contentCardViewModel.Title}");

        var queuingContentCardViewModel = queuingContentCardViewModels
            .FirstOrDefault(s => s.Title.Equals(title));

        if (queuingContentCardViewModel == null)
        {
            MessageBox.Show($"Cannot find queuing video view model with title: {title}");
            return;
        }

        var remove = queuingContentCardViewModels
            .Remove(
                queuingContentCardViewModel
            );
        MessageBox.Show($"View Model: {title} is removed? {remove}");

        var foundQueuingVideo = _unitOfWork.QueuingContentRepository
            .Find(v => v.Title.Equals(title))
            .FirstOrDefault();

        if (foundQueuingVideo == null)
        {
            MessageBox.Show($"Cannot find queuing video with title: {title}");
            return;
        }

        MessageBox.Show($"Found Queuing Video {foundQueuingVideo}");

        _unitOfWork.QueuingContentRepository
            .Remove(foundQueuingVideo);

        if (_unitOfWork.Complete() == 0) return;
        MessageBox.Show("Queuing Video Deleted From DB");
        CheckQueue(mediaClippexViewModel);
    }

    public static void CheckQueue(MediaClippexViewModel mediaClippexViewModel = default!)
    {
        mediaClippexViewModel.HasQueue = mediaClippexViewModel.QueuingContentCardViewModels.Count > 0;
    }

    public static void CheckDownloadHistory(MediaClippexViewModel mediaClippexViewModel = default!)
    {
        mediaClippexViewModel.HasDownloadHistory = mediaClippexViewModel.DownloadedVideoCardViewModels.Count > 0;
    }
}