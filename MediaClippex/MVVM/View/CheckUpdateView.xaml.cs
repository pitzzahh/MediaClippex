using MediaClippex.MVVM.ViewModel;

namespace MediaClippex.MVVM.View;

public partial class CheckUpdateView
{
    
    public CheckUpdateView()
    {
        InitializeComponent();
        DataContext = new CheckUpdateViewModel();
    }

}