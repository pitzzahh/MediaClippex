using System;
using MediaClippex.DB.Core;
using MediaClippex.DB.Persistence.Repositories;

namespace MediaClippex.DB.Persistence;

// ReSharper disable once ClassNeverInstantiated.Global
public class UnitOfWork : IUnitOfWork
{
    private readonly MediaClippexDataContext _context;

    public UnitOfWork(MediaClippexDataContext context)
    {
        _context = context;
        VideosRepository = new VideosRepository(_context);
    }

    public VideosRepository VideosRepository { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public int Complete()
    {
        return _context.SaveChangesAsync().Result;
    }
}