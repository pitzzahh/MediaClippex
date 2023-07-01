using MediaClippex.MVVM.ViewModel;

namespace MediaClippex.MVVM.View;

public partial class CheckForUpdatesView
{
    public CheckForUpdatesView()
    {
        InitializeComponent();
        DataContext = new CheckForUpdatesViewModel();

    }
}