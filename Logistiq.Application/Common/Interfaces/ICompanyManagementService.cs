using Logistiq.Domain.Entities;
using Logistiq.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Application.Common.Interfaces
{
    public interface ICompanyManagementService
    {
        Task<Company> CreateCompanyForUserAsync(Guid userId, string companyName);
        Task<CompanyUser> AddUserToCompanyAsync(Guid userId, Guid companyId, CompanyUserRole role = CompanyUserRole.User);
        Task<Company?> GetUserActiveCompanyAsync(Guid userId);
    }
}
