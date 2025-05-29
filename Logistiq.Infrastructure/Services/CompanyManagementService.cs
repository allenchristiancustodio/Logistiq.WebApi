using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;
using Logistiq.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Infrastructure.Services
{
    public class CompanyManagementService : ICompanyManagementService
    {
        private readonly IRepository<Company> _companyRepository;
        private readonly IRepository<CompanyUser> _companyUserRepository;
        private readonly IRepository<ApplicationUser> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CompanyManagementService> _logger;

        public CompanyManagementService(
            IRepository<Company> companyRepository,
            IRepository<CompanyUser> companyUserRepository,
            IRepository<ApplicationUser> userRepository,
            IUnitOfWork unitOfWork,
            ILogger<CompanyManagementService> logger)
        {
            _companyRepository = companyRepository;
            _companyUserRepository = companyUserRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Company> CreateCompanyForUserAsync(Guid userId, string companyName)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Create the company
                var company = new Company
                {
                    Name = companyName,
                    IsActive = true
                };

                await _companyRepository.AddAsync(company);
                await _unitOfWork.SaveChangesAsync();

                // Add user as owner
                var companyUser = new CompanyUser
                {
                    ApplicationUserId = userId,
                    CompanyId = company.Id,
                    Role = CompanyUserRole.Owner,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                };

                await _companyUserRepository.AddAsync(companyUser);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Created company {CompanyName} for user {UserId}", companyName, userId);
                return company;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CompanyUser> AddUserToCompanyAsync(Guid userId, Guid companyId, CompanyUserRole role = CompanyUserRole.User)
        {
            // Check if user is already in company
            var existingCompanyUser = await _companyUserRepository.FirstOrDefaultAsync(
                cu => cu.ApplicationUserId == userId && cu.CompanyId == companyId);

            if (existingCompanyUser != null)
            {
                existingCompanyUser.IsActive = true;
                existingCompanyUser.Role = role;
                await _companyUserRepository.UpdateAsync(existingCompanyUser);
            }
            else
            {
                existingCompanyUser = new CompanyUser
                {
                    ApplicationUserId = userId,
                    CompanyId = companyId,
                    Role = role,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                };
                await _companyUserRepository.AddAsync(existingCompanyUser);
            }

            await _unitOfWork.SaveChangesAsync();
            return existingCompanyUser;
        }

        public async Task<Company?> GetUserActiveCompanyAsync(Guid userId)
        {
            var companyUser = await _companyUserRepository.FirstOrDefaultAsync(
                cu => cu.ApplicationUserId == userId && cu.IsActive);

            if (companyUser == null) return null;

            return await _companyRepository.GetByIdAsync(companyUser.CompanyId);
        }
    }
}
