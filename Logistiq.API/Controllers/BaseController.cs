// Logistiq.API/Controllers/BaseController.cs
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Logistiq.API.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly ILogger _logger;

    protected BaseController(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extract organization ID from JWT claims, handling the nested JSON structure
    /// </summary>
    protected string? GetOrganizationIdFromClaims()
    {
        // Method 1: Try direct org_id claim first
        var orgId = User.FindFirst("org_id")?.Value;
        if (!string.IsNullOrEmpty(orgId) && !IsTemplateValue(orgId))
        {
            _logger.LogDebug("Found organization ID in direct claim: {OrgId}", orgId);
            return orgId;
        }

        // Method 2: Parse from 'o' claim (organization object) - THIS IS THE KEY ONE
        var orgClaim = User.FindFirst("o")?.Value;
        if (!string.IsNullOrEmpty(orgClaim))
        {
            try
            {
                _logger.LogDebug("Parsing organization from 'o' claim: {OrgClaim}", orgClaim);
                var orgData = JsonSerializer.Deserialize<JsonElement>(orgClaim);

                if (orgData.TryGetProperty("id", out var idElement))
                {
                    orgId = idElement.GetString();
                    if (!string.IsNullOrEmpty(orgId) && !IsTemplateValue(orgId))
                    {
                        _logger.LogDebug("Found organization ID in 'o' claim: {OrgId}", orgId);
                        return orgId;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse organization claim 'o': {OrgClaim}", orgClaim);
            }
        }

        // Method 3: Try alternative organization_id claim
        orgId = User.FindFirst("organization_id")?.Value;
        if (!string.IsNullOrEmpty(orgId) && !IsTemplateValue(orgId))
        {
            _logger.LogDebug("Found organization ID in alternative claim: {OrgId}", orgId);
            return orgId;
        }

        // Log all available claims for debugging
        var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        _logger.LogWarning("No organization ID found. Available claims: {Claims}",
            JsonSerializer.Serialize(allClaims));

        return null;
    }

    /// <summary>
    /// Get organization ID and return BadRequest if not found
    /// </summary>
    protected ActionResult<string> GetRequiredOrganizationId()
    {
        var organizationId = GetOrganizationIdFromClaims();
        if (string.IsNullOrEmpty(organizationId))
        {
            _logger.LogWarning("Organization context required but not found. User: {UserId}",
                User.FindFirst("sub")?.Value ?? User.FindFirst("clerkId")?.Value);
            return BadRequest(new
            {
                error = "Organization context required. Please ensure you are in an organization.",
                hint = "Make sure you're signed into an organization in Clerk"
            });
        }
        return organizationId;
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    protected string? GetCurrentUserId()
    {
        return User.FindFirst("clerkId")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Get current user email from claims
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst("email")?.Value
            ?? User.FindFirst("emailaddress")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Check if a value contains template syntax
    /// </summary>
    protected static bool IsTemplateValue(string value)
    {
        return value.Contains("{{") || value.Contains("}}") || value == "{{org.id}}";
    }
}