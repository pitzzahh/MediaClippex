using MediaClippexUpdaterViewModel = Elevator.MVVM.ViewModel.MediaClippexUpdaterViewModel;

namespace Elevator.MVVM.View;

public partial class MediaClippexUpdater
{
    public MediaClippexUpdater()
    {
        InitializeComponent();
        DataContext = new MediaClippexUpdaterViewModel();
    }
}