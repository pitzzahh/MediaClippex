using System.Linq;
using System.Threading.Tasks;
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

        // Needs to run on the Current dispatcher in order to remove the view models
        Application.Current.Dispatcher.Invoke(() =>
        {
            var queuingContentCardViewModels = mediaClippexViewModel
                .QueuingContentCardViewModels;

            var queuingContentCardViewModel = queuingContentCardViewModels
                .FirstOrDefault(s => title.Equals(s.Title));

            if (queuingContentCardViewModel == null) return;

            // Remove view model from item source
            queuingContentCardViewModels
                .Remove(
                    queuingContentCardViewModel
                );

            var foundQueuingVideo = _unitOfWork.QueuingContentRepository
                .Find(v => v.Title.Equals(title))
                .FirstOrDefault();

            if (foundQueuingVideo == null) return;

            _unitOfWork.QueuingContentRepository
                .Remove(foundQueuingVideo);

            if (_unitOfWork.Complete() == 0) return;
            _unitOfWork.Dispose();
            Task.Run(mediaClippexViewModel.GetQueuingVideos);
        });
    }
}