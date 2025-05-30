using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;
using Logistiq.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Persistence.Repositories
{
    public class UserRepository : Repository<ApplicationUser>, IUserRepository
    {
        public UserRepository(LogistiqDbContext context) : base(context)
        {
        }

        public async Task<ApplicationUser?> GetUserWithCompaniesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.CompanyUsers)
                    .ThenInclude(cu => cu.Company)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        // Updated method name and implementation for Clerk
        public async Task<ApplicationUser?> GetUserWithCompaniesByClerkIdAsync(string clerkUserId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.CompanyUsers)
                    .ThenInclude(cu => cu.Company)
                .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId, cancellationToken);
        }
    }
}