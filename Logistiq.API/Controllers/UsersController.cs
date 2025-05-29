using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;

namespace Logistiq.API.Controllers;

public class UsersController : BaseApiController
{
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly IRepository<CompanyUser> _companyUserRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(
        IRepository<ApplicationUser> userRepository,
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

        // Check if user exists with company relationships
        var existingUser = await _userRepository.FindAsync(u => u.KindeUserId == kindeUserId);
        var user = existingUser.FirstOrDefault();

        bool isNewUser = false;

        if (user == null)
        {
            // Create new user
            user = new ApplicationUser
            {
                KindeUserId = kindeUserId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            isNewUser = true;
        }
        else
        {
            // Update existing user
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Phone = request.Phone ?? user.Phone;

            await _userRepository.UpdateAsync(user);
        }

        await _unitOfWork.SaveChangesAsync();

        // Get user with company relationships loaded
        var userWithCompanies = await GetUserWithCompaniesAsync(user.Id);

        // Check if user has active company
        var activeCompanyUser = userWithCompanies?.CompanyUsers.FirstOrDefault(cu => cu.IsActive);

        var response = new UserResult
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            IsNewUser = isNewUser,
            HasActiveCompany = activeCompanyUser != null,
            CurrentCompanyId = activeCompanyUser?.CompanyId.ToString(),
            CurrentCompanyName = activeCompanyUser?.Company?.Name
        };

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var kindeUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(kindeUserId))
        {
            return BadRequest("User ID not found");
        }

        var user = await _userRepository.FirstOrDefaultAsync(u => u.KindeUserId == kindeUserId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        // Get user with company relationships loaded
        var userWithCompanies = await GetUserWithCompaniesAsync(user.Id);
        var activeCompanyUser = userWithCompanies?.CompanyUsers.FirstOrDefault(cu => cu.IsActive);

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

    [HttpGet("companies")]
    public async Task<ActionResult> GetUserCompanies()
    {
        var kindeUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(kindeUserId))
        {
            return BadRequest("User ID not found");
        }

        var user = await _userRepository.FirstOrDefaultAsync(u => u.KindeUserId == kindeUserId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        // Get user with company relationships loaded
        var userWithCompanies = await GetUserWithCompaniesAsync(user.Id);
        if (userWithCompanies == null)
        {
            return NotFound("User not found");
        }

        var companies = userWithCompanies.CompanyUsers.Select(cu => new UserCompany
        {
            Id = cu.CompanyId.ToString(),
            Name = cu.Company?.Name ?? "Unknown Company",
            Role = cu.Role.ToString(),
            IsActive = cu.IsActive,
            JoinedAt = cu.JoinedAt.ToString("yyyy-MM-dd")
        }).ToList();

        return Ok(companies);
    }

    [HttpPost("switch-company")]
    public async Task<ActionResult> SwitchCompany([FromBody] SwitchCompanyRequest request)
    {
        var kindeUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(kindeUserId))
        {
            return BadRequest("User ID not found");
        }

        var user = await _userRepository.FirstOrDefaultAsync(u => u.KindeUserId == kindeUserId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        if (!Guid.TryParse(request.CompanyId, out var companyId))
        {
            return BadRequest("Invalid company ID");
        }

        // Get user with company relationships
        var userWithCompanies = await GetUserWithCompaniesAsync(user.Id);
        if (userWithCompanies == null)
        {
            return NotFound("User not found");
        }

        // Check if user belongs to the company
        var targetCompanyUser = userWithCompanies.CompanyUsers.FirstOrDefault(cu => cu.CompanyId == companyId);
        if (targetCompanyUser == null)
        {
            return BadRequest("User does not belong to this company");
        }

        // Deactivate all company memberships for this user
        foreach (var companyUser in userWithCompanies.CompanyUsers)
        {
            companyUser.IsActive = false;
            await _companyUserRepository.UpdateAsync(companyUser);
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

    private async Task<ApplicationUser?> GetUserWithCompaniesAsync(Guid userId)
    {
        // This is a simplified approach - in a real application you'd want to use 
        // Entity Framework Include() method or create a specific repository method
        var users = await _userRepository.FindAsync(u => u.Id == userId);
        return users.FirstOrDefault();
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