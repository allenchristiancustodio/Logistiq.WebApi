using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Common.Models;
using Logistiq.Domain.Common;
using Logistiq.Persistence.Data;

namespace Logistiq.Persistence.Repositories;

public class OrganizationRepository<TEntity, TId> : Repository<TEntity, TId>, IOrganizationRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>, IOrganizationEntity
{
    public OrganizationRepository(LogistiqDbContext context) : base(context)
    {
    }

    public async Task<TEntity?> GetByIdAsync(TId id, string organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id!.Equals(id) && e.ClerkOrganizationId == organizationId, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.ClerkOrganizationId == organizationId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, string organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.ClerkOrganizationId == organizationId).Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, string organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.ClerkOrganizationId == organizationId).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, string organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.ClerkOrganizationId == organizationId).AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(string organizationId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => e.ClerkOrganizationId == organizationId);

        return predicate == null
            ? await query.CountAsync(cancellationToken)
            : await query.CountAsync(predicate, cancellationToken);
    }

    public async Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, string organizationId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => e.ClerkOrganizationId == organizationId);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class OrganizationRepository<TEntity> : OrganizationRepository<TEntity, Guid>, IOrganizationRepository<TEntity>
    where TEntity : BaseEntity, IOrganizationEntity
{
    public OrganizationRepository(LogistiqDbContext context) : base(context)
    {
    }
}