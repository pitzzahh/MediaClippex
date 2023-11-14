using System.Linq;
using System.Windows;
using MediaClippex.DB.Core;
using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.Services;

[Service(Scope.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public class StorageService
{
    private readonly IServicesContainer _container;
    private readonly IUnitOfWork _unitOfWork;

    public StorageService(IUnitOfWork unitOfWork, IServicesContainer container)
    {
        _unitOfWork = unitOfWork;
        _container = container;
    }

    public void RemoveFromQueue(QueuingContentCardViewModel vm)
    {
        // Needs to run on the Current dispatcher in order to remove the view models
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var foundQueuingVideo = _unitOfWork.QueuingContentRepository
                    .Find(v => v.Title.Equals(vm.Title))
                    .FirstOrDefault();

                if (foundQueuingVideo == null) return;

                _unitOfWork.QueuingContentRepository.Remove(foundQueuingVideo);
            }
            finally
            {
                _unitOfWork.Complete();
                _unitOfWork.Dispose();
                var homeViewModel = _container.Resolve<HomeViewModel>();
                homeViewModel.QueuingContentCardViewModels.Remove(vm);
                homeViewModel.HasQueue = homeViewModel.QueuingContentCardViewModels.Count > 0;
            }
        });
    }

    public void AddToDownloadHistory(DownloadedContentCardViewModel vm)
    {
        // Needs to run on the Current dispatcher in order to remove the view models
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var viewModel = _container.Resolve<HomeViewModel>();
            viewModel
                .DownloadedVideoCardViewModels
                .Insert(0, vm);
            viewModel.HasDownloadHistory = true;
        });
    }
}