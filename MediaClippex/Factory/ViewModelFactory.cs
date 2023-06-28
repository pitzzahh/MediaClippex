using MediaClippex.MVVM.ViewModel;

namespace MediaClippex.Factory;

public static class ViewModelFactory
{
    public static MediaClippexViewModel CreateMediaClippexViewModel()
    {
        return new MediaClippexViewModel();
    }
}