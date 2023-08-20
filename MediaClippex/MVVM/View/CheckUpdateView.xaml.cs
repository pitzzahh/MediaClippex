using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.MVVM.View;

public partial class CheckUpdateView
{
    public CheckUpdateView()
    {
        InitializeComponent();
        DataContext = BuilderServices.Resolve<CheckUpdateViewModel>();
    }

}