using Logistiq.Application.Users.DTOs;

namespace Logistiq.Application.Users;

public interface IUserService
{
    Task<UserResponse> SyncUserAsync(SyncUserRequest request);
    Task<UserResponse> UpdateUserProfileAsync(UpdateUserProfileRequest request);
    Task<UserResponse> CompleteUserOnboardingAsync(CompleteUserOnboardingRequest request);
    Task<UserResponse?> GetCurrentUserAsync();
    
}
