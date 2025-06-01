using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Organizations;
using Logistiq.Application.Organizations.DTOs;
using Logistiq.Domain.Entities;
using Logistiq.Persistence.Data;
using Microsoft.Extensions.Logging;

namespace Logistiq.Infrastructure.Services;

public class OrganizationService : IOrganizationService
{
    private readonly LogistiqDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(LogistiqDbContext context, ICurrentUserService currentUser, ILogger<OrganizationService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<OrganizationResponse> SyncOrganizationAsync(SyncOrganizationRequest request)
    {
        var clerkOrgId = _currentUser.OrganizationId;

        // If no organization context, try to find organization by name (for immediate post-creation sync)
        if (string.IsNullOrEmpty(clerkOrgId))
        {
            _logger.LogWarning("No organization context found, attempting to find by name: {Name}", request.Name);

            // Try to find the most recently created organization with this name
            var recentOrg = await _context.Organizations
                .Where(o => o.Name == request.Name)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (recentOrg != null)
            {
                _logger.LogInformation("Found organization by name: {OrgId} - {Name}", recentOrg.ClerkOrganizationId, recentOrg.Name);
                return MapToResponse(recentOrg);
            }

            throw new UnauthorizedAccessException("No organization context found and unable to locate organization by name");
        }

        var existingOrg = await _context.Organizations
            .FirstOrDefaultAsync(o => o.ClerkOrganizationId == clerkOrgId);

        if (existingOrg == null)
        {
            // Create new organization
            existingOrg = new Organization
            {
                ClerkOrganizationId = clerkOrgId,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                Industry = request.Industry,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Website = request.Website,
                IsActive = true
            };

            _context.Organizations.Add(existingOrg);
            _logger.LogInformation("Creating new organization: {OrgId} - {Name}", clerkOrgId, request.Name);
        }
        else
        {
            // Update existing organization
            existingOrg.Name = request.Name;
            existingOrg.Slug = request.Slug ?? existingOrg.Slug;
            existingOrg.Description = request.Description ?? existingOrg.Description;
            existingOrg.ImageUrl = request.ImageUrl ?? existingOrg.ImageUrl;
            existingOrg.Industry = request.Industry ?? existingOrg.Industry;
            existingOrg.Address = request.Address ?? existingOrg.Address;
            existingOrg.Phone = request.Phone ?? existingOrg.Phone;
            existingOrg.Email = request.Email ?? existingOrg.Email;
            existingOrg.Website = request.Website ?? existingOrg.Website;

            _logger.LogInformation("Updating existing organization: {OrgId} - {Name}", clerkOrgId, request.Name);
        }

        await _context.SaveChangesAsync();

        return MapToResponse(existingOrg);
    }

    public async Task<OrganizationResponse?> GetCurrentOrganizationAsync()
    {
        var clerkOrgId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(clerkOrgId))
            return null;

        return await GetOrganizationByClerkIdAsync(clerkOrgId);
    }

    public async Task<OrganizationResponse?> GetOrganizationByClerkIdAsync(string clerkOrganizationId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.ClerkOrganizationId == clerkOrganizationId);

        return organization == null ? null : MapToResponse(organization);
    }

    private static OrganizationResponse MapToResponse(Organization org)
    {
        return new OrganizationResponse
        {
            Id = org.Id.ToString(),
            ClerkOrganizationId = org.ClerkOrganizationId,
            Name = org.Name,
            Slug = org.Slug,
            Description = org.Description,
            ImageUrl = org.ImageUrl,
            Industry = org.Industry,
            Address = org.Address,
            Phone = org.Phone,
            Email = org.Email,
            Website = org.Website,
            IsActive = org.IsActive,
            CreatedAt = org.CreatedAt
        };
    }
}