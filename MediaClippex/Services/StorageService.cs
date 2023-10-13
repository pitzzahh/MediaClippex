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

        // Needs to run on the Current dispatcher in order to remove the view models
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
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

                mediaClippexViewModel.HasQueue = mediaClippexViewModel.QueuingContentCardViewModels.Count > 0;
            }
            finally
            {
                _unitOfWork.Complete();
                _unitOfWork.Dispose();
            }
        });
    }
}