using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Common.Models;
using Logistiq.Domain.Common;
using Logistiq.Persistence.Data;

namespace Logistiq.Persistence.Repositories;

public class TenantRepository<TEntity, TId> : Repository<TEntity, TId>, ITenantRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>, ITenantEntity
{
    public TenantRepository(LogistiqDbContext context) : base(context)
    {
    }

    public async Task<TEntity?> GetByIdAsync(TId id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id!.Equals(id) && e.CompanyId == companyId, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.CompanyId == companyId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.CompanyId == companyId).Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.CompanyId == companyId).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.CompanyId == companyId).AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(Guid companyId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => e.CompanyId == companyId);

        return predicate == null
            ? await query.CountAsync(cancellationToken)
            : await query.CountAsync(predicate, cancellationToken);
    }

    public async Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, Guid companyId, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => e.CompanyId == companyId);

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

public class TenantRepository<TEntity> : TenantRepository<TEntity, Guid>, ITenantRepository<TEntity> where TEntity : BaseEntity, ITenantEntity
{
    public TenantRepository(LogistiqDbContext context) : base(context)
    {
    }
}