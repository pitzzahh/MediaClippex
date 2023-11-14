using System;
using MediaClippex.DB.Core;
using MediaClippex.DB.Persistence.Repositories;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.DB.Persistence;

[Service(Scope.Transient, Registration.AsSelfAndInterfaces)]
public class UnitOfWork : IUnitOfWork
{
    private readonly MediaClippexDataContext _context;

    public UnitOfWork(MediaClippexDataContext context)
    {
        _context = context;
        VideosRepository = new VideosRepository(_context);
        QueuingContentRepository = new QueuingContentRepository(_context);
    }

    public VideosRepository VideosRepository { get; }
    public QueuingContentRepository QueuingContentRepository { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public int Complete()
    {
        return _context.SaveChangesAsync().Result;
    }
}