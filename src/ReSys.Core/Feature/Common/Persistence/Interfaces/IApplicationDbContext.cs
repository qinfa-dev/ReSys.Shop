using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ReSys.Core.Feature.Common.Persistence.Interfaces;

/// <summary>
/// Provides access to database entities and query operations.
/// For persistence operations, use IUnitOfWork.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<T> Set<T>() where T : class;
    DatabaseFacade Database { get; }
}
