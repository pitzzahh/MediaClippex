using System;
using MediaClippex.DB.Persistence.Repositories;

namespace MediaClippex.DB.Core;

public interface IUnitOfWork : IDisposable
{
    VideosRepository VideosRepository { get; }
    int Complete();
}