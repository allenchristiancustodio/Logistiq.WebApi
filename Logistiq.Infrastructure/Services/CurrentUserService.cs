using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Logistiq.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            // Try primary org_id claim first
            var orgId = httpContext.User.FindFirstValue("org_id");

            // If template value, treat as null
            if (!string.IsNullOrEmpty(orgId) && (orgId.Contains("{{") || orgId == "{{org.id}}"))
            {
                _logger.LogWarning("Organization ID contains template value: {OrgId}", orgId);
                orgId = null;
            }

            // Try alternative claim locations if primary is null
            if (string.IsNullOrEmpty(orgId))
            {
                orgId = httpContext.User.FindFirstValue("organization_id");
            }

            // Try parsing from 'o' claim if still null
            if (string.IsNullOrEmpty(orgId))
            {
                var orgClaim = httpContext.User.FindFirstValue("o");
                if (!string.IsNullOrEmpty(orgClaim))
                {
                    try
                    {
                        var orgData = JsonSerializer.Deserialize<JsonElement>(orgClaim);
                        if (orgData.TryGetProperty("id", out var idElement))
                        {
                            orgId = idElement.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore parsing errors
                    }
                }
            }

            _logger.LogDebug("Retrieved organization ID: {OrgId}", orgId);
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