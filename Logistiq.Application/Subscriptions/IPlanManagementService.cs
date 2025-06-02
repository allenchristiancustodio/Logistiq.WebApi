using Logistiq.Application.Subscriptions.DTOs;

namespace Logistiq.Application.Subscriptions;

public interface IPlanManagementService
{
    Task<SubscriptionResponse> ChangePlanAsync(ChangePlanRequest request);
    Task<SubscriptionResponse> UpgradeToProAsync(bool isAnnual = false);
    Task<SubscriptionResponse> DowngradeToStarterAsync();
    Task<bool> CanChangeToPlanAsync(string planId);
    Task<List<string>> GetUpgradeRecommendationsAsync();
}