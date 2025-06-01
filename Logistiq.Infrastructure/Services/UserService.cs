using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Users;
using Logistiq.Application.Users.DTOs;
using Logistiq.Domain.Entities;
using Logistiq.Persistence.Data;

namespace Logistiq.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly LogistiqDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UserService(LogistiqDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserResponse> SyncUserAsync(SyncUserRequest request)
    {
        var clerkUserId = _currentUser.UserId;
        if (string.IsNullOrEmpty(clerkUserId))
            throw new UnauthorizedAccessException("User not authenticated");

        var existingUser = await _context.ApplicationUsers
            .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId);

        if (existingUser == null)
        {
            // Create new user
            existingUser = new ApplicationUser
            {
                ClerkUserId = clerkUserId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                ImageUrl = request.ImageUrl,
                Preferences = request.Preferences,
                IsActive = true,
                CurrentOrganizationId = _currentUser.OrganizationId,
                LastSeenAt = DateTime.UtcNow,
                HasCompletedOnboarding = false
            };

            _context.ApplicationUsers.Add(existingUser);
        }
        else
        {
            // Update existing user with all fields
            existingUser.Email = request.Email;
            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.Phone = request.Phone ?? existingUser.Phone;
            existingUser.ImageUrl = request.ImageUrl ?? existingUser.ImageUrl;
            existingUser.Preferences = request.Preferences ?? existingUser.Preferences;
            existingUser.CurrentOrganizationId = _currentUser.OrganizationId;
            existingUser.LastSeenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return MapToUserResponse(existingUser);
    }

    // Add new method for profile updates
    public async Task<UserResponse> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        var clerkUserId = _currentUser.UserId;
        if (string.IsNullOrEmpty(clerkUserId))
            throw new UnauthorizedAccessException("User not authenticated");

        var user = await _context.ApplicationUsers
            .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Update profile fields
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Preferences = request.Preferences;
        user.LastSeenAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToUserResponse(user);
    }

    public async Task<UserResponse> CompleteUserOnboardingAsync(CompleteUserOnboardingRequest request)
    {
        var clerkUserId = _currentUser.UserId;
        if (string.IsNullOrEmpty(clerkUserId))
            throw new UnauthorizedAccessException("User not authenticated");

        var user = await _context.ApplicationUsers
            .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Update profile fields
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Preferences = request.Preferences;
        user.LastSeenAt = DateTime.UtcNow;

        // Mark onboarding as completed
        user.HasCompletedOnboarding = true;
        user.OnboardingCompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToUserResponse(user);
    }

    public async Task<UserResponse?> GetCurrentUserAsync()
    {
        var clerkUserId = _currentUser.UserId;
        if (string.IsNullOrEmpty(clerkUserId))
            return null;

        var user = await _context.ApplicationUsers
            .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId);

        if (user == null)
            return null;

        // Update organization context if needed
        if (user.CurrentOrganizationId != _currentUser.OrganizationId)
        {
            user.CurrentOrganizationId = _currentUser.OrganizationId;
            await _context.SaveChangesAsync();
        }

        return new UserResponse
        {
            Id = user.Id.ToString(),
            ClerkUserId = user.ClerkUserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            CurrentOrganizationId = user.CurrentOrganizationId,
            Phone = user.Phone,
            ImageUrl = user.ImageUrl,
            IsActive = user.IsActive
        };
    }

    private static UserResponse MapToUserResponse(ApplicationUser user)
    {
        return new UserResponse
        {
            Id = user.Id.ToString(),
            ClerkUserId = user.ClerkUserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            CurrentOrganizationId = user.CurrentOrganizationId,
            Phone = user.Phone,
            ImageUrl = user.ImageUrl,
            Preferences = user.Preferences,
            IsActive = user.IsActive,
            LastSeenAt = user.LastSeenAt,
            CreatedAt = user.CreatedAt,
            HasCompletedOnboarding = user.HasCompletedOnboarding,
            OnboardingCompletedAt = user.OnboardingCompletedAt
        };
    }
}