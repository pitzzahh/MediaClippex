using System.Collections.ObjectModel;
using System.Linq;
using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class StorageService
{
    public readonly ObservableCollection<DownloadedVideoCardViewModel> DownloadedVideoCardViewModelsList = new();

    public readonly ObservableCollection<QueuingContentCardViewModel> QueuingContentCardViewModelsList = new();

    public void RemoveFromQueue(string title)
    {
        var queuingContentCardViewModel = BuilderServices.Resolve<StorageService>()
            .QueuingContentCardViewModelsList
            .FirstOrDefault(s => s.Title.Equals(title));
        if (queuingContentCardViewModel != null)
            QueuingContentCardViewModelsList
                .Remove(
                    queuingContentCardViewModel
                );
    }
}