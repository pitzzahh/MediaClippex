using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using MediaClippex.DB.Core;
using MediaClippex.MVVM.ViewModel;
using MediaClippex.Services;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.MVVM.View;

/// <summary>
///     Interaction logic for MainView.xaml
/// </summary>
[Service]
public partial class MainView
{
    private readonly IServicesContainer _container;

    public MainView(MainViewModel mainViewModel, IServicesContainer container)
    {
        _container = container;
        InitializeComponent();
        DataContext = mainViewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var unitOfWork = _container.Resolve<IUnitOfWork>();
        var queuingContents = unitOfWork.QueuingContentRepository.GetAll().ToList();
        var queuingContentsCount = queuingContents.Count;
        var hasQueue = queuingContentsCount > 0;
        if (hasQueue)
        {
            var messageBoxResult = MessageBox.Show(
                $"You have {queuingContentsCount} remaining queue.\nSave queue? the queue will re-download upon next launch",
                "Notice",
                MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (messageBoxResult is MessageBoxResult.Yes)
                foreach (var queuingContent in queuingContents)
                    queuingContent.Paused = true;
            else
            {
                foreach (var queuingContent in queuingContents)
                    unitOfWork.QueuingContentRepository.Remove(queuingContent);

                foreach (var vm in _container.Resolve<HomeViewModel>().QueuingContentCardViewModels.ToList())
                {
                    _container.Resolve<StorageService>().RemoveFromQueue(vm);
                    vm.CancellationTokenSource.Cancel();
                }
            }

            unitOfWork.Complete();
        }

        Application.Current.Shutdown();
        Environment.Exit(0);
        base.OnClosing(e);
    }
}