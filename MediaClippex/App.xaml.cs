﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MediaClippex.MVVM.View;
using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Helpers;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex;

public partial class App
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DownloadFFmpeg.ps1")}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            var process = new Process { StartInfo = processStartInfo };
            process.Start();
            await process.WaitForExitAsync();

            var servicesContainer = new ServicesCollection()
                .AddServices()
                .AddServicesFromReferenceAssemblies()
                .Build();

            servicesContainer.Resolve<MainView>().Show();

            var homeViewModel = servicesContainer.Resolve<HomeViewModel>();
            homeViewModel.GetQueuingVideos();
            homeViewModel.GetDownloadedVideos();
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error downloading FFmpeg: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            Current.Shutdown(7);
            Environment.Exit(7);
        }
    }
}