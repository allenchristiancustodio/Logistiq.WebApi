using Logistiq.Application.Common.Models;
using Logistiq.Domain.Common;
using Logistiq.Domain.Entities;
using System.Linq.Expressions;

namespace Logistiq.Application.Common.Interfaces
{
    public interface IOrganizationRepository<TEntity, TId> : IRepository<TEntity, TId>
        where TEntity : BaseEntity<TId>, IOrganizationEntity
    {
        Task<TEntity?> GetByIdAsync(TId id, string organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetAllAsync(string organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, string organizationId, CancellationToken cancellationToken = default);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, string organizationId, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, string organizationId, CancellationToken cancellationToken = default);
        Task<int> CountAsync(string organizationId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, string organizationId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    }

    public interface IOrganizationRepository<TEntity> : IOrganizationRepository<TEntity, Guid>
        where TEntity : BaseEntity, IOrganizationEntity
    {
    }
}