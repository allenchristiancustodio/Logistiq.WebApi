using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;
using Logistiq.Domain.Enums;

namespace Logistiq.API.Controllers;

public class UsersController : BaseApiController
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<CompanyUser> _companyUserRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(
        IUserRepository userRepository,
        IRepository<CompanyUser> companyUserRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _companyUserRepository = companyUserRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("create-or-update")]
    public async Task<ActionResult> CreateOrUpdateUser([FromBody] CreateOrUpdateUserRequest request)
    {
        var kindeUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(kindeUserId))
        {
            return BadRequest("User ID not found");
        }

        try
        {
            // Check if user exists with company relationships
            var existingUser = await _userRepository.GetUserWithCompaniesByKindeIdAsync(kindeUserId);

            bool isNewUser = false;

            if (existingUser == null)
            {
                // Create new user
                existingUser = new ApplicationUser
                {
                    KindeUserId = kindeUserId,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    IsActive = true
                };

                await _userRepository.AddAsync(existingUser);
                isNewUser = true;
            }
            else
            {
                // Update existing user
                existingUser.Email = request.Email;
                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.Phone = request.Phone ?? existingUser.Phone;

                await _userRepository.UpdateAsync(existingUser);
            }

            await _unitOfWork.SaveChangesAsync();

            // Reload user with updated company relationships if not new user
            if (!isNewUser)
            {
                existingUser = await _userRepository.GetUserWithCompaniesAsync(existingUser.Id);
            }

            // Check if user has active company
            var activeCompanyUser = existingUser?.CompanyUsers?.FirstOrDefault(cu => cu.IsActive);

            var response = new UserResult
            {
                UserId = existingUser.Id.ToString(),
                Email = existingUser.Email,
                FullName = $"{existingUser.FirstName} {existingUser.LastName}".Trim(),
                IsNewUser = isNewUser,
                HasActiveCompany = activeCompanyUser != null,
                CurrentCompanyId = activeCompanyUser?.CompanyId.ToString(),
                CurrentCompanyName = activeCompanyUser?.Company?.Name
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating/updating user: {ex.Message}");
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var kindeUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(kindeUserId))
        {
            return BadRequest("User ID not found");
        }

        try
        {
            var user = await _userRepository.GetUserWithCompaniesByKindeIdAsync(kindeUserId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var activeCompanyUser = user.CompanyUsers?.FirstOrDefault(cu => cu.IsActive);

            var response = new UserResult
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                IsNewUser = false,
                HasActiveCompany = activeCompanyUser != null,
                CurrentCompanyId = activeCompanyUser?.CompanyId.ToString(),
                CurrentCompanyName = activeCompanyUser?.Company?.Name
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error getting current user: {ex.Message}");
        }
    }

    [HttpGet("companies")]
    public async Task<ActionResult> GetUserCompanies()
    {
        var kindeUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(kindeUserId))
        {
            return BadRequest("User ID not found");
        }

        try
        {
            var user = await _userRepository.GetUserWithCompaniesByKindeIdAsync(kindeUserId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var companies = user.CompanyUsers?.Where(cu => cu.Company.IsActive)
                .Select(cu => new UserCompany
                {
                    Id = cu.CompanyId.ToString(),
                    Name = cu.Company?.Name ?? "Unknown Company",
                    Role = cu.Role.ToString(),
                    IsActive = cu.IsActive,
                    JoinedAt = cu.JoinedAt.ToString("yyyy-MM-dd")
                }).ToList() ?? new List<UserCompany>();

            return Ok(companies);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error getting user companies: {ex.Message}");
        }
    }

    [HttpPost("switch-company")]
    public async Task<ActionResult> SwitchCompany([FromBody] SwitchCompanyRequest request)
    {
        var kindeUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(kindeUserId))
        {
            return BadRequest("User ID not found");
        }

        if (!Guid.TryParse(request.CompanyId, out var companyId))
        {
            return BadRequest("Invalid company ID");
        }

        try
        {
            var user = await _userRepository.GetUserWithCompaniesByKindeIdAsync(kindeUserId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check if user belongs to the company
            var targetCompanyUser = user.CompanyUsers?.FirstOrDefault(cu => cu.CompanyId == companyId);
            if (targetCompanyUser == null)
            {
                return BadRequest("User does not belong to this company");
            }

            // Deactivate all company memberships for this user
            if (user.CompanyUsers != null)
            {
                foreach (var companyUser in user.CompanyUsers)
                {
                    companyUser.IsActive = false;
                    await _companyUserRepository.UpdateAsync(companyUser);
                }
            }

            // Activate the target company
            targetCompanyUser.IsActive = true;
            await _companyUserRepository.UpdateAsync(targetCompanyUser);

            await _unitOfWork.SaveChangesAsync();

            var response = new UserResult
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                IsNewUser = false,
                HasActiveCompany = true,
                CurrentCompanyId = targetCompanyUser.CompanyId.ToString(),
                CurrentCompanyName = targetCompanyUser.Company?.Name ?? "Unknown Company"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error switching company: {ex.Message}");
        }
    }
}

public class CreateOrUpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UserResult
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsNewUser { get; set; }
    public bool HasActiveCompany { get; set; }
    public string? CurrentCompanyId { get; set; }
    public string? CurrentCompanyName { get; set; }
}

public class UserCompany
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string JoinedAt { get; set; } = string.Empty;
}

public class SwitchCompanyRequest
{
    public string CompanyId { get; set; } = string.Empty;
}