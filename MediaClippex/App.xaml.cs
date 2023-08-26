﻿using System;
using System.Diagnostics;
using System.IO;
using MediaClippex.MVVM.ViewModel;
using MediaClippex.Services;
using Russkyc.AttachedUtilities.FileStreamExtensions;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public App()
    {
        var ffmpegFile = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpegFile))
        {
            Process.Start(Path.Combine(AppContext.BaseDirectory, "Launcher.exe"));
            Current.Shutdown();
        }

        BuilderServices.BuildWithContainer(BuildContainer.ConfigureServices());
        var versionFilePath = Path.Combine(AppContext.BaseDirectory, "app.version");
        if (!File.Exists(versionFilePath))
        {
            versionFilePath.StreamWrite(CheckUpdateViewModel.ReadCurrentVersion());
        }
    }
}