using MediaClippex.MVVM.Model;
using Microsoft.EntityFrameworkCore;

namespace MediaClippex.DB.Persistence.Repositories;

public class QueuingContentRepository : Repository<QueuingContent>
{
    public QueuingContentRepository(DbContext context) : base(context)
    {
    }
}