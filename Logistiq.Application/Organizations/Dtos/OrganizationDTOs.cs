namespace Logistiq.Application.Organizations.DTOs;

public class SyncOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Industry { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public bool? MultiLocationEnabled { get; set; }
}

public class UpdateOrganizationRequest
{
    public string? Description { get; set; }
    public string? Industry { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public bool? MultiLocationEnabled { get; set; }
}

public class OrganizationResponse
{
    public string Id { get; set; } = string.Empty;
    public string ClerkOrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Industry { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public bool MultiLocationEnabled { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool HasCompletedSetup { get; set; }
    public DateTime? SetupCompletedAt { get; set; }
}

public class CompleteOrganizationSetupRequest
{
    public string? Description { get; set; }
    public string? Industry { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public bool? MultiLocationEnabled { get; set; }
}