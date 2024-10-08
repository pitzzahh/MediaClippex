﻿$ErrorActionPreference = "Stop"
$ffmpegFilePath = "$PSScriptRoot/ffmpeg.exe"

if (Test-Path $ffmpegFilePath)
{
    Write-Host "Skipped downloading FFmpeg, file already exists."
    exit
}

Write-Host "Downloading FFmpeg..."

$webClient = New-Object System.Net.WebClient

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

try
{
    $webClient.DownloadFile("https://github.com/Tyrrrz/FFmpegBin/releases/latest/download/ffmpeg-windows-x64.zip", "$ffmpegFilePath.zip")
}
finally
{
    $webClient.Dispose()
}

try
{
    Add-Type -Assembly System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead("$ffmpegFilePath.zip")
    try
    {
        [IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry("ffmpeg.exe"), $ffmpegFilePath)
    }
    finally
    {
        $zip.Dispose()
    }

    Write-Host "Done downloading FFmpeg."
}
finally
{
    # Clean up
    Remove-Item "$ffmpegFilePath.zip" -Force
}
