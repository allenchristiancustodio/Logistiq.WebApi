using Logistiq.Domain.Common;
using Logistiq.Application.Common.Models;
using System.Linq.Expressions;

namespace Logistiq.Application.Common.Interfaces
{
    public interface ITenantRepository<TEntity, TId> : IRepository<TEntity, TId>
        where TEntity : BaseEntity<TId>, ITenantEntity
    {
        Task<TEntity?> GetByIdAsync(TId id, Guid companyId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Guid companyId, CancellationToken cancellationToken = default);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Guid companyId, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Guid companyId, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Guid companyId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, Guid companyId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    }

    public interface ITenantRepository<TEntity> : ITenantRepository<TEntity, Guid> where TEntity : BaseEntity, ITenantEntity
    {
    }
}
