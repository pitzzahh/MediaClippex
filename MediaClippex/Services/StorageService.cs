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
        var mediaClippexViewModel = BuilderServices.Resolve<MediaClippexViewModel>();
        var queuingContentCardViewModels = mediaClippexViewModel.QueuingContentCardViewModels;
        queuingContentCardViewModels
            .Remove(
                mediaClippexViewModel
                    .QueuingContentCardViewModels
                    .First(s => s.Title.Equals(title))
            );

        var foundQueuingVideo = _unitOfWork.QueuingContentRepository
            .Find(v => v.Title.Equals(title))
            .FirstOrDefault();
        MessageBox.Show($"Found Queuing Video {foundQueuingVideo}");

        if (foundQueuingVideo == null) return;
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