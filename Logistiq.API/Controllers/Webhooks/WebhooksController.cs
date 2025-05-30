using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Domain.Entities;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

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

    public WebhooksController(
        ILogger<WebhooksController> logger,
        IRepository<ApplicationUser> userRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _logger = logger;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
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

            _logger.LogInformation("Received Clerk webhook payload length: {Length}", body.Length);

            if (!await VerifySvixSignature(body))
            {
                _logger.LogWarning("Invalid Svix webhook signature");
                return Unauthorized("Invalid signature");
            }

            var webhookData = JsonSerializer.Deserialize<ClerkWebhookEvent>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (webhookData == null)
            {
                _logger.LogError("Failed to parse webhook data");
                return BadRequest("Invalid webhook data");
            }

            _logger.LogInformation("Processing Clerk webhook: {Type} for user {UserId}",
                webhookData.Type, webhookData.Data?.Id);

            // Handle different event types
            var result = webhookData.Type switch
            {
                "user.created" => await HandleUserCreated(webhookData.Data),
                "user.updated" => await HandleUserUpdated(webhookData.Data),
                _ => HandleUnknownEvent(webhookData.Type)
            };

            if (!result)
            {
                return StatusCode(500, new { error = "Failed to process webhook" });
            }

            return Ok(new
            {
                message = "Webhook processed successfully",
                eventType = webhookData.Type,
                userId = webhookData.Data?.Id,
                timestamp = DateTime.UtcNow
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in webhook");
            return BadRequest(new { error = "Invalid JSON payload" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Clerk webhook");
            return StatusCode(500, new { error = "Internal server error" });
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

    private async Task<bool> HandleUserCreated(ClerkUserData userData)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.ClerkUserId == userData.Id); // Updated
            if (existingUser != null)
            {
                _logger.LogInformation("User already exists: {UserId}", userData.Id);
                return true;
            }

            // Get email
            var email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress;
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("No email found for user: {UserId}", userData.Id);
                return false;
            }

            // Create user
            var newUser = new ApplicationUser
            {
                ClerkUserId = userData.Id,
                Email = email,
                FirstName = userData.FirstName ?? "",
                LastName = userData.LastName ?? "",
                IsActive = true
            };

            await _userRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created user: {ClerkUserId} - {Email}", userData.Id, email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {UserId}", userData.Id);
            return false;
        }
    }

    private async Task<bool> HandleUserUpdated(ClerkUserData userData)
    {
        try
        {
            var user = await _userRepository.FirstOrDefaultAsync(u => u.ClerkUserId == userData.Id); // Updated
            if (user == null)
            {
                // User doesn't exist, create it
                return await HandleUserCreated(userData);
            }

            // Update user info
            var email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress;
            if (!string.IsNullOrEmpty(email))
            {
                user.Email = email;
                user.FirstName = userData.FirstName ?? user.FirstName;
                user.LastName = userData.LastName ?? user.LastName;

                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated user: {ClerkUserId}", userData.Id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user: {UserId}", userData.Id);
            return false;
        }
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
    public object? Object { get; set; } 
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