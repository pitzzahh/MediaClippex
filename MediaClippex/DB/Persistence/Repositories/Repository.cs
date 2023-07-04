namespace MediaClippex.DB.Persistence.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public abstract class Repository<T> where T : class
{
    private readonly DbContext _context;

    protected Repository(DbContext context)
    {
        _context = context;
        new DatabaseFacade(context).EnsureCreatedAsync();
    }

    public void Add(T entity)
    {
        _context.Set<T>().Add(entity);
    }

    public void Remove(T expression)
    {
        _context.Set<T>().Remove(expression);
    }

    public T? GetById(int id)
    {
        return _context.Set<T>().Find(id);
    }

    public IEnumerable<T> Find(Expression<Func<T, bool>> expression)
    {
        return _context.Set<T>().Where(expression);
    }

    public IEnumerable<T> GetAll()
    {
        return _context.Set<T>().ToList();
    }
}