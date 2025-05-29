using Microsoft.AspNetCore.Mvc;
using Logistiq.Application.Companies.Commands.CreateCompany;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;

namespace Logistiq.API.Controllers;

public class CompaniesController : BaseApiController
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public CompaniesController(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    [HttpGet("current")]
    public async Task<ActionResult> GetCurrentCompany()
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
            if (activeCompanyUser?.Company == null)
            {
                return NotFound("No active company found");
            }

            var company = activeCompanyUser.Company;
            var response = new CurrentCompanyResponse
            {
                Id = company.Id.ToString(),
                Name = company.Name,
                Description = company.Description,
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                Website = company.Website,
                IsActive = company.IsActive,
                UserRole = activeCompanyUser.Role.ToString(),
                JoinedAt = activeCompanyUser.JoinedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error getting current company: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<ActionResult> CreateCompany([FromBody] CreateCompanyCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                var response = new CreateCompanyApiResponse
                {
                    Id = result.Value.CompanyId.ToString(),
                    Name = result.Value.CompanyName,
                    IsOwner = true
                };
                return Ok(response);
            }

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating company: {ex.Message}");
        }
    }
}

public class CurrentCompanyResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class CreateCompanyApiResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
}