using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Logistiq.Application.Organizations.DTOs;
using Logistiq.Application.Organizations;
using Logistiq.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Logistiq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly LogistiqDbContext _context; // Direct DB access for webhooks

    public WebhooksController(
        ILogger<WebhooksController> logger,
        IRepository<ApplicationUser> userRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        LogistiqDbContext context)
    {
        _logger = logger;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("clerk")]
    public async Task<IActionResult> HandleClerkWebhook()
    {
        try
        {
            // Read the raw body
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            _logger.LogInformation("Received Clerk webhook payload: {Body}", body);

            if (!await VerifySvixSignature(body))
            {
                _logger.LogWarning("Invalid Svix webhook signature");
                return Unauthorized("Invalid signature");
            }

            // Parse as generic JSON first to see the structure
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var eventType = root.GetProperty("type").GetString();
            var eventData = root.GetProperty("data");

            _logger.LogInformation("Processing Clerk webhook: {Type} with data: {Data}",
                eventType, eventData.GetRawText());

            var result = eventType switch
            {
                "user.created" => await HandleUserCreated(eventData),
                "user.updated" => await HandleUserUpdated(eventData),
                "organization.created" => await HandleOrganizationCreated(eventData),
                "organization.updated" => await HandleOrganizationUpdated(eventData),
                _ => HandleUnknownEvent(eventType)
            };

            if (!result)
            {
                return StatusCode(500, new { error = "Failed to process webhook" });
            }

            return Ok(new
            {
                message = "Webhook processed successfully",
                eventType = eventType,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Clerk webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task<bool> HandleOrganizationCreated(JsonElement orgData)
    {
        try
        {
            var orgId = orgData.GetProperty("id").GetString();
            var orgName = orgData.GetProperty("name").GetString();
            var orgSlug = orgData.TryGetProperty("slug", out var slugProp) ? slugProp.GetString() : null;

            _logger.LogInformation("Creating organization: {OrgId} - {Name} - {Slug}",
                orgId, orgName, orgSlug);

            // Check if organization already exists
            var existingOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ClerkOrganizationId == orgId);

            if (existingOrg != null)
            {
                _logger.LogInformation("Organization already exists: {OrgId}", orgId);
                return true;
            }

            // Create organization directly in database (bypassing service layer for webhooks)
            var newOrganization = new Organization
            {
                ClerkOrganizationId = orgId!,
                Name = orgName!,
                Slug = orgSlug,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(newOrganization);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created organization via webhook: {OrgId} - {Name}", orgId, orgName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create organization via webhook");
            return false;
        }
    }

    private async Task<bool> HandleOrganizationUpdated(JsonElement orgData)
    {
        try
        {
            var orgId = orgData.GetProperty("id").GetString();
            var orgName = orgData.GetProperty("name").GetString();
            var orgSlug = orgData.TryGetProperty("slug", out var slugProp) ? slugProp.GetString() : null;

            _logger.LogInformation("Updating organization: {OrgId} - {Name}", orgId, orgName);

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.ClerkOrganizationId == orgId);

            if (organization == null)
            {
                // Organization doesn't exist, create it
                return await HandleOrganizationCreated(orgData);
            }

            // Update organization
            organization.Name = orgName!;
            organization.Slug = orgSlug ?? organization.Slug;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated organization via webhook: {OrgId}", orgId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update organization via webhook");
            return false;
        }
    }

    private async Task<bool> HandleUserCreated(JsonElement userData)
    {
        try
        {
            var userId = userData.GetProperty("id").GetString();
            var firstName = userData.TryGetProperty("first_name", out var fnProp) ? fnProp.GetString() : "";
            var lastName = userData.TryGetProperty("last_name", out var lnProp) ? lnProp.GetString() : "";

            // Check if user already exists
            var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.ClerkUserId == userId);
            if (existingUser != null)
            {
                _logger.LogInformation("User already exists: {UserId}", userId);
                return true;
            }

            // Get email from email_addresses array
            string? email = null;
            if (userData.TryGetProperty("email_addresses", out var emailsProp) && emailsProp.ValueKind == JsonValueKind.Array)
            {
                var primaryEmailId = userData.TryGetProperty("primary_email_address_id", out var primaryProp)
                    ? primaryProp.GetString() : null;

                foreach (var emailElement in emailsProp.EnumerateArray())
                {
                    var emailId = emailElement.GetProperty("id").GetString();
                    var emailAddress = emailElement.GetProperty("email_address").GetString();

                    if (emailId == primaryEmailId || email == null)
                    {
                        email = emailAddress;
                        if (emailId == primaryEmailId) break; // Use primary if found
                    }
                }
            }

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("No email found for user: {UserId}", userId);
                return true; // Don't fail webhook
            }

            // Create user
            var newUser = new ApplicationUser
            {
                ClerkUserId = userId!,
                Email = email,
                FirstName = firstName ?? "",
                LastName = lastName ?? "",
                IsActive = true
            };

            await _userRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created user via webhook: {ClerkUserId} - {Email}", userId, email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user via webhook");
            return false;
        }
    }

    private async Task<bool> HandleUserUpdated(JsonElement userData)
    {
        try
        {
            var userId = userData.GetProperty("id").GetString();

            var user = await _userRepository.FirstOrDefaultAsync(u => u.ClerkUserId == userId);
            if (user == null)
            {
                // User doesn't exist, try to create it
                _logger.LogInformation("User not found for update, attempting to create: {UserId}", userId);
                return await HandleUserCreated(userData);
            }

            // Update user fields
            if (userData.TryGetProperty("first_name", out var fnProp))
            {
                user.FirstName = fnProp.GetString() ?? user.FirstName;
            }

            if (userData.TryGetProperty("last_name", out var lnProp))
            {
                user.LastName = lnProp.GetString() ?? user.LastName;
            }

            // Update email if available
            if (userData.TryGetProperty("email_addresses", out var emailsProp) && emailsProp.ValueKind == JsonValueKind.Array)
            {
                var primaryEmailId = userData.TryGetProperty("primary_email_address_id", out var primaryProp)
                    ? primaryProp.GetString() : null;

                foreach (var emailElement in emailsProp.EnumerateArray())
                {
                    var emailId = emailElement.GetProperty("id").GetString();
                    var emailAddress = emailElement.GetProperty("email_address").GetString();

                    if (emailId == primaryEmailId && !string.IsNullOrEmpty(emailAddress))
                    {
                        user.Email = emailAddress;
                        break;
                    }
                }
            }

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated user via webhook: {ClerkUserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user via webhook");
            return false;
        }
    }

    private async Task<bool> VerifySvixSignature(string body)
    {
        try
        {
            var webhookSecret = _configuration["Clerk:WebhookSecret"];

            // For local development with ngrok, you might want to skip verification
            if (string.IsNullOrEmpty(webhookSecret))
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    _logger.LogWarning("Skipping webhook signature verification in development");
                    return true;
                }
                _logger.LogError("No webhook secret configured");
                return false;
            }

            // Get Svix headers
            var svixId = Request.Headers["svix-id"].FirstOrDefault();
            var svixTimestamp = Request.Headers["svix-timestamp"].FirstOrDefault();
            var svixSignature = Request.Headers["svix-signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(svixId) || string.IsNullOrEmpty(svixTimestamp) || string.IsNullOrEmpty(svixSignature))
            {
                _logger.LogWarning("Missing Svix headers");
                return false;
            }

            // Verify timestamp (prevent replay attacks)
            if (long.TryParse(svixTimestamp, out var timestamp))
            {
                var webhookTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                var now = DateTimeOffset.UtcNow;
                var tolerance = TimeSpan.FromMinutes(5); // 5 minute tolerance

                if (Math.Abs((now - webhookTime).TotalMinutes) > tolerance.TotalMinutes)
                {
                    _logger.LogWarning("Webhook timestamp too old: {WebhookTime}, Now: {Now}", webhookTime, now);
                    return false;
                }
            }

            // Verify signature using Svix algorithm
            var expectedSignature = GenerateSvixSignature(svixId, svixTimestamp, body, webhookSecret);

            // Svix signatures come in format "v1,signature1 v1,signature2"
            var signatures = svixSignature.Split(' ');
            foreach (var sig in signatures)
            {
                if (sig.StartsWith("v1,") && sig.Equals($"v1,{expectedSignature}", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            _logger.LogWarning("Signature verification failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    private string GenerateSvixSignature(string svixId, string svixTimestamp, string body, string secret)
    {
        var payload = $"{svixId}.{svixTimestamp}.{body}";
        var secretBytes = Convert.FromBase64String(secret.Replace("whsec_", ""));

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private bool HandleUnknownEvent(string eventType)
    {
        _logger.LogInformation("Received unhandled webhook event type: {EventType}", eventType);
        return true;
    }

    [HttpGet("health")]
    public IActionResult WebhookHealth()
    {
        return Ok(new
        {
            status = "healthy",
            service = "clerk-webhooks",
            timestamp = DateTime.UtcNow
        });
    }
}

// Webhook data models (same as before but with better validation)
public class ClerkWebhookEvent
{
    public string Type { get; set; } = string.Empty;
    public ClerkUserData? Data { get; set; }
    public ClerkOrganizationData? OrgData { get; set; }
    public object? Object { get; set; }
}

public class ClerkOrganizationData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public string? ImageUrl { get; set; }
}

public class ClerkUserData
{
    public string Id { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PrimaryEmailAddressId { get; set; }
    public List<ClerkEmailAddress>? EmailAddresses { get; set; }
    public List<ClerkPhoneNumber>? PhoneNumbers { get; set; }
    public long CreatedAt { get; set; } 
    public long UpdatedAt { get; set; }
    public string? ImageUrl { get; set; }
    public bool? HasImage { get; set; }
}

public class ClerkEmailAddress
{
    public string Id { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public ClerkVerification? Verification { get; set; }
}

public class ClerkPhoneNumber
{
    public string Id { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public ClerkVerification? Verification { get; set; }
}

public class ClerkVerification
{
    public string Status { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
}