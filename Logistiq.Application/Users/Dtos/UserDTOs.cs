namespace Logistiq.Application.Users.DTOs;

public class SyncUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ImageUrl { get; set; }
    public string? Preferences { get; set; }
}

public class UpdateUserProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Preferences { get; set; }
}

public class UserResponse
{
    public string Id { get; set; } = string.Empty;
    public string ClerkUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CurrentOrganizationId { get; set; }
    public string? Phone { get; set; }
    public string? ImageUrl { get; set; }
    public string? Preferences { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool HasCompletedOnboarding { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }
}

public class CompleteUserOnboardingRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Preferences { get; set; }
}