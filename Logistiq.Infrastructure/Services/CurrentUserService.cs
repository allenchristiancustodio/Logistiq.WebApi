using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Logistiq.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Logistiq.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("clerkId")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("email")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? OrganizationId
    {
        get
        {
            var orgId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("org_id");
            if (!string.IsNullOrEmpty(orgId) && (orgId.Contains("{{") || orgId == "{{org.id}}"))
            {
                _logger.LogWarning("Organization ID contains template value: {OrgId}. User may not be in organization context.", orgId);
                return null; // Treat template values as no organization
            }

            _logger.LogDebug("Retrieved organization ID from claims: {OrgId}", orgId);
            return orgId;
        }
    }

    public async Task<string?> GetCurrentOrganizationIdAsync()
    {
        return OrganizationId;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        return UserId;
    }
}