using Logistiq.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Application.Common.Interfaces
{
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        Task<ApplicationUser?> GetUserWithCompaniesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetUserWithCompaniesByClerkIdAsync(string kindeUserId, CancellationToken cancellationToken = default);
    }
}
