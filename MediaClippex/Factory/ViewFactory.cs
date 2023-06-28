using MediaClippex.MVVM.View;

namespace MediaClippex.Factory;

public static class ViewFactory
{
    public static void CreateMediaClippexView()
    {
        new MediaClippexView
        {
            DataContext = ViewModelFactory.CreateMediaClippexViewModel()
        }.Show();
    }
}