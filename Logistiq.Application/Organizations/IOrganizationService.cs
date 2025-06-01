using Logistiq.Application.Organizations.DTOs;

namespace Logistiq.Application.Organizations;

public interface IOrganizationService
{
    Task<OrganizationResponse> SyncOrganizationAsync(SyncOrganizationRequest request);
    Task<OrganizationResponse> UpdateOrganizationAsync(UpdateOrganizationRequest request);
    Task<OrganizationResponse> CompleteOrganizationSetupAsync(CompleteOrganizationSetupRequest request);
    Task<OrganizationResponse?> GetCurrentOrganizationAsync();
    Task<OrganizationResponse?> GetOrganizationByClerkIdAsync(string clerkOrganizationId);
}
