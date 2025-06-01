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
        if (string.IsNullOrEmpty(clerkOrgId))
            throw new UnauthorizedAccessException("No organization context found");

        var existingOrg = await _context.Organizations
            .FirstOrDefaultAsync(o => o.ClerkOrganizationId == clerkOrgId);

        if (existingOrg == null)
        {
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
                TaxId = request.TaxId,
                BusinessRegistrationNumber = request.BusinessRegistrationNumber,
                DefaultCurrency = request.DefaultCurrency ?? "USD",
                TimeZone = request.TimeZone ?? "UTC",
                DateFormat = request.DateFormat ?? "MM/dd/yyyy",
                MultiLocationEnabled = request.MultiLocationEnabled ?? false,
                IsActive = true,
                HasCompletedSetup = false
            };

            _context.Organizations.Add(existingOrg);
        }
        else
        {
            existingOrg.Name = request.Name;
            existingOrg.Slug = request.Slug ?? existingOrg.Slug;
            existingOrg.Description = request.Description ?? existingOrg.Description;
            existingOrg.ImageUrl = request.ImageUrl ?? existingOrg.ImageUrl;
            existingOrg.Industry = request.Industry ?? existingOrg.Industry;
            existingOrg.Address = request.Address ?? existingOrg.Address;
            existingOrg.Phone = request.Phone ?? existingOrg.Phone;
            existingOrg.Email = request.Email ?? existingOrg.Email;
            existingOrg.Website = request.Website ?? existingOrg.Website;
            existingOrg.TaxId = request.TaxId ?? existingOrg.TaxId;
            existingOrg.BusinessRegistrationNumber = request.BusinessRegistrationNumber ?? existingOrg.BusinessRegistrationNumber;
            existingOrg.DefaultCurrency = request.DefaultCurrency ?? existingOrg.DefaultCurrency;
            existingOrg.TimeZone = request.TimeZone ?? existingOrg.TimeZone;
            existingOrg.DateFormat = request.DateFormat ?? existingOrg.DateFormat;
            existingOrg.MultiLocationEnabled = request.MultiLocationEnabled ?? existingOrg.MultiLocationEnabled;
        }

        await _context.SaveChangesAsync();
        return MapToOrganizationResponse(existingOrg);
    }

    // Add new method for organization updates
    public async Task<OrganizationResponse> UpdateOrganizationAsync(UpdateOrganizationRequest request)
    {
        var clerkOrgId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(clerkOrgId))
            throw new UnauthorizedAccessException("No organization context found");

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.ClerkOrganizationId == clerkOrgId);

        if (organization == null)
            throw new KeyNotFoundException("Organization not found");

        // Update all provided fields
        if (request.Description != null) organization.Description = request.Description;
        if (request.Industry != null) organization.Industry = request.Industry;
        if (request.Address != null) organization.Address = request.Address;
        if (request.Phone != null) organization.Phone = request.Phone;
        if (request.Email != null) organization.Email = request.Email;
        if (request.Website != null) organization.Website = request.Website;
        if (request.TaxId != null) organization.TaxId = request.TaxId;
        if (request.BusinessRegistrationNumber != null) organization.BusinessRegistrationNumber = request.BusinessRegistrationNumber;
        if (request.DefaultCurrency != null) organization.DefaultCurrency = request.DefaultCurrency;
        if (request.TimeZone != null) organization.TimeZone = request.TimeZone;
        if (request.DateFormat != null) organization.DateFormat = request.DateFormat;
        if (request.MultiLocationEnabled.HasValue) organization.MultiLocationEnabled = request.MultiLocationEnabled.Value;

        await _context.SaveChangesAsync();
        return MapToOrganizationResponse(organization);
    }

    public async Task<OrganizationResponse> CompleteOrganizationSetupAsync(CompleteOrganizationSetupRequest request)
    {
        var clerkOrgId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(clerkOrgId))
            throw new UnauthorizedAccessException("No organization context found");

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.ClerkOrganizationId == clerkOrgId);

        if (organization == null)
            throw new KeyNotFoundException("Organization not found");

        // Update all provided fields
        if (request.Description != null) organization.Description = request.Description;
        if (request.Industry != null) organization.Industry = request.Industry;
        if (request.Address != null) organization.Address = request.Address;
        if (request.Phone != null) organization.Phone = request.Phone;
        if (request.Email != null) organization.Email = request.Email;
        if (request.Website != null) organization.Website = request.Website;
        if (request.TaxId != null) organization.TaxId = request.TaxId;
        if (request.BusinessRegistrationNumber != null) organization.BusinessRegistrationNumber = request.BusinessRegistrationNumber;
        if (request.DefaultCurrency != null) organization.DefaultCurrency = request.DefaultCurrency;
        if (request.TimeZone != null) organization.TimeZone = request.TimeZone;
        if (request.DateFormat != null) organization.DateFormat = request.DateFormat;
        if (request.MultiLocationEnabled.HasValue) organization.MultiLocationEnabled = request.MultiLocationEnabled.Value;

        // Mark setup as completed
        organization.HasCompletedSetup = true;
        organization.SetupCompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToOrganizationResponse(organization);
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

        return organization == null ? null : MapToOrganizationResponse(organization);
    }

    private static OrganizationResponse MapToOrganizationResponse(Organization org)
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
            TaxId = org.TaxId,
            BusinessRegistrationNumber = org.BusinessRegistrationNumber,
            DefaultCurrency = org.DefaultCurrency,
            TimeZone = org.TimeZone,
            DateFormat = org.DateFormat,
            MultiLocationEnabled = org.MultiLocationEnabled,
            MaxUsers = org.MaxUsers,
            MaxProducts = org.MaxProducts,
            IsActive = org.IsActive,
            CreatedAt = org.CreatedAt,
            HasCompletedSetup = org.HasCompletedSetup,
            SetupCompletedAt = org.SetupCompletedAt
        };
    }
}