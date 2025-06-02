using Logistiq.Application.Subscriptions.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Application.Subscriptions
{
    public interface ISubscriptionService
    {
        Task<SubscriptionResponse?> GetCurrentSubscriptionAsync();
        Task<SubscriptionResponse> CreateTrialSubscriptionAsync(CreateTrialSubscriptionRequest request);
        Task<SubscriptionResponse> CreatePaidSubscriptionAsync(CreatePaidSubscriptionRequest request);
        Task<SubscriptionResponse> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequest request);
        Task CancelSubscriptionAsync(Guid id, CancelSubscriptionRequest request);
        Task<SubscriptionResponse> ReactivateSubscriptionAsync(Guid id);
        Task<List<SubscriptionPlanResponse>> GetAvailablePlansAsync();
        Task<SubscriptionLimitsResponse> GetSubscriptionLimitsAsync();
        Task<bool> CheckLimitAsync(SubscriptionLimitType limitType, int currentCount);
        Task<SubscriptionUsageResponse> GetUsageStatsAsync();
    }
}
