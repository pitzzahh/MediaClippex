﻿namespace MediaClippex.Services;

public class DownloadService
{
    private readonly int _chunkSize;
    private readonly string _destination;
    private readonly bool _disposeOnCompletion;
    private readonly IProgress<double> _progress;
    private readonly string _sourceUrl;
    private volatile bool _allowedToRun;
    private Lazy<int> _contentLength;
    private Stream _sourceStream;

    public DownloadService(Stream source, string destination, bool disposeOnCompletion = true,
        int chunkSizeInBytes = 10000 /*Default to 0.01 mb*/, IProgress<double> progress = null)
    {
        _allowedToRun = true;

        _sourceStream = source;
        _destination = destination;
        _disposeOnCompletion = disposeOnCompletion;
        _chunkSize = chunkSizeInBytes;
        _contentLength = new Lazy<int>(() => Convert.ToInt32(GetContentLength()));
        _progress = progress;

        BytesWritten = 0;
    }

    public FileDownload(string source, string destination, int chunkSizeInBytes = 10000 /*Default to 0.01 mb*/,
        IProgress<double> progress = null)
    {
        _allowedToRun = true;

        _sourceUrl = source;
        _destination = destination;
        _chunkSize = chunkSizeInBytes;
        _contentLength = new Lazy<int>(() => Convert.ToInt32(GetContentLength()));
        _progress = progress;

        BytesWritten = 0;
    }

    public int BytesWritten { get; private set; }
    public int ContentLength => _contentLength.Value;

    public bool Done => ContentLength == BytesWritten;

    private long GetContentLength()
    {
        if (_sourceStream != null)
        {
            return _sourceStream.Length;
        }

        var request = (HttpWebRequest)WebRequest.Create(_sourceUrl);
        request.Method = "HEAD";

        using (var response = request.GetResponse())
        {
            return response.ContentLength;
        }
    }

    private async Task Start(int range)
    {
        if (!_allowedToRun)
            throw new InvalidOperationException();

        if (_sourceStream != null)
        {
            using (var fs = new FileStream(_destination, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                while (_allowedToRun)
                {
                    var buffer = new byte[_chunkSize];
                    var bytesRead = await _sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                    if (bytesRead == 0) break;

                    await fs.WriteAsync(buffer, 0, bytesRead);
                    BytesWritten += bytesRead;
                    _progress?.Report((double)BytesWritten / ContentLength);
                }

                await fs.FlushAsync();
            }

            //Control whether the stream should be disposed here or outside of this class
            if (BytesWritten == ContentLength && _disposeOnCompletion)
                _sourceStream?.Dispose();
        }
        else
        {
            var request = (HttpWebRequest)WebRequest.Create(_sourceUrl);
            request.Method = "GET";
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
            request.AddRange(range);

            using (var response = await request.GetResponseAsync())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var fs = new FileStream(_destination, FileMode.Append, FileAccess.Write,
                               FileShare.ReadWrite))
                    {
                        while (_allowedToRun)
                        {
                            var buffer = new byte[_chunkSize];
                            var bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)
                                .ConfigureAwait(false);

                            if (bytesRead == 0) break;

                            await fs.WriteAsync(buffer, 0, bytesRead);
                            BytesWritten += bytesRead;
                            _progress?.Report((double)BytesWritten / ContentLength);
                        }

                        await fs.FlushAsync();
                    }
                }
            }
        }
    }

    public Task Start()
    {
        _allowedToRun = true;
        return Start(BytesWritten);
    }

    public void Pause()
    {
        _allowedToRun = false;
    }
}