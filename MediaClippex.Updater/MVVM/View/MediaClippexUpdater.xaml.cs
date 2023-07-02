using MediaClippex.Updater.MVVM.ViewModel;

namespace MediaClippex.Updater.MVVM.View;

public partial class MediaClippexUpdater
{
    public MediaClippexUpdater()
    {
        InitializeComponent();
        DataContext = new MediaClippexUpdaterViewModel();
    }
}