using Logistiq.Application.Organizations.DTOs;

namespace Logistiq.Application.Organizations;

public interface IOrganizationService
{
    Task<OrganizationResponse> SyncOrganizationAsync(SyncOrganizationRequest request);
    Task<OrganizationResponse?> GetCurrentOrganizationAsync();
    Task<OrganizationResponse?> GetOrganizationByClerkIdAsync(string clerkOrganizationId);
}
