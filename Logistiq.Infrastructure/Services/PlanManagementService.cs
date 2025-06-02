using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Subscriptions;
using Logistiq.Application.Subscriptions.DTOs;
using Logistiq.Application.Payments;
using Logistiq.Domain.Entities;
using Logistiq.Domain.Enums;
using Logistiq.Persistence.Data;

namespace Logistiq.Infrastructure.Services;

public class PlanManagementService : IPlanManagementService
{
    private readonly LogistiqDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PlanManagementService> _logger;
    private readonly IStripeService _stripeService;
    private readonly ISubscriptionService _subscriptionService; // This is safe now

    public PlanManagementService(
        LogistiqDbContext context,
        ICurrentUserService currentUser,
        ILogger<PlanManagementService> logger,
        IStripeService stripeService,
        ISubscriptionService subscriptionService)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
        _stripeService = stripeService;
        _subscriptionService = subscriptionService;
    }

    public async Task<SubscriptionResponse> ChangePlanAsync(ChangePlanRequest request)
    {
        var organizationId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(organizationId))
            throw new UnauthorizedAccessException("No organization context found");

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.ClerkOrganizationId == organizationId);

        if (subscription == null)
            throw new KeyNotFoundException("Subscription not found");

        if (string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            throw new InvalidOperationException("No Stripe subscription found");

        try
        {
            // Update Stripe subscription first
            var stripeSubscription = await _stripeService.ChangePlanAsync(subscription.StripeSubscriptionId, request);

            // Update local subscription
            subscription.PlanName = GetPlanNameFromId(request.NewPlanId);
            subscription.StripePriceId = request.StripePriceId;
            subscription.MonthlyPrice = stripeSubscription.Amount;

            // Update plan limits based on new plan
            SetPlanLimits(subscription, request.NewPlanId);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully changed plan for organization {OrganizationId} to {NewPlan}",
                organizationId, request.NewPlanId);

            // Get updated subscription response
            return await _subscriptionService.GetCurrentSubscriptionAsync()
                ?? throw new InvalidOperationException("Failed to retrieve updated subscription");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change plan for organization {OrganizationId}", organizationId);
            throw new InvalidOperationException($"Failed to change plan: {ex.Message}", ex);
        }
    }

    public async Task<SubscriptionResponse> UpgradeToProAsync(bool isAnnual = false)
    {
        var availablePlans = await _subscriptionService.GetAvailablePlansAsync();
        var proPlan = availablePlans.FirstOrDefault(p => p.Id == "professional");

        if (proPlan == null)
            throw new InvalidOperationException("Professional plan not found");

        var request = new ChangePlanRequest
        {
            NewPlanId = "professional",
            StripePriceId = isAnnual ? proPlan.StripePriceIdAnnual : proPlan.StripePriceIdMonthly,
            IsAnnual = isAnnual,
            ProrateBilling = true
        };

        return await ChangePlanAsync(request);
    }

    public async Task<SubscriptionResponse> DowngradeToStarterAsync()
    {
        var availablePlans = await _subscriptionService.GetAvailablePlansAsync();
        var starterPlan = availablePlans.FirstOrDefault(p => p.Id == "starter");

        if (starterPlan == null)
            throw new InvalidOperationException("Starter plan not found");

        // Check if current usage is within starter limits
        var usage = await _subscriptionService.GetUsageStatsAsync();
        var starterLimits = new { MaxUsers = 5, MaxProducts = 500, MaxOrders = 1000, MaxWarehouses = 2 };

        if (usage.CurrentUsers > starterLimits.MaxUsers ||
            usage.CurrentProducts > starterLimits.MaxProducts ||
            usage.CurrentWarehouses > starterLimits.MaxWarehouses)
        {
            throw new InvalidOperationException("Current usage exceeds Starter plan limits. Please reduce usage before downgrading.");
        }

        var request = new ChangePlanRequest
        {
            NewPlanId = "starter",
            StripePriceId = starterPlan.StripePriceIdMonthly,
            IsAnnual = false,
            ProrateBilling = true
        };

        return await ChangePlanAsync(request);
    }

    public async Task<bool> CanChangeToPlanAsync(string planId)
    {
        var usage = await _subscriptionService.GetUsageStatsAsync();
        var availablePlans = await _subscriptionService.GetAvailablePlansAsync();
        var targetPlan = availablePlans.FirstOrDefault(p => p.Id == planId);

        if (targetPlan == null)
            return false;

        // Check if current usage fits within the target plan limits
        return usage.CurrentUsers <= targetPlan.MaxUsers &&
               usage.CurrentProducts <= targetPlan.MaxProducts &&
               usage.CurrentOrders <= targetPlan.MaxOrders &&
               usage.CurrentWarehouses <= targetPlan.MaxWarehouses;
    }

    public async Task<List<string>> GetUpgradeRecommendationsAsync()
    {
        var usage = await _subscriptionService.GetUsageStatsAsync();
        var recommendations = new List<string>();

        // Check if approaching limits
        foreach (var metric in usage.UsageMetrics)
        {
            if (metric.Value.IsNearLimit || metric.Value.IsAtLimit)
            {
                recommendations.Add($"You're approaching your {metric.Key.ToLower()} limit. Consider upgrading for more capacity.");
            }
        }

        // Add feature-based recommendations
        var currentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync();
        if (currentSubscription != null)
        {
            if (!currentSubscription.HasAdvancedReporting)
            {
                recommendations.Add("Upgrade to Professional for advanced reporting and analytics.");
            }

            if (!currentSubscription.HasInvoicing)
            {
                recommendations.Add("Upgrade to Professional for invoicing and billing features.");
            }
        }

        return recommendations;
    }

    private string GetPlanNameFromId(string planId)
    {
        return planId switch
        {
            "starter" => "Starter",
            "professional" => "Professional",
            "enterprise" => "Enterprise",
            _ => "Professional" // Default fallback
        };
    }

    private void SetPlanLimits(Subscription subscription, string planId)
    {
        switch (planId.ToLower())
        {
            case "starter":
                subscription.MaxUsers = 5;
                subscription.MaxProducts = 500;
                subscription.MaxOrders = 1000;
                subscription.MaxWarehouses = 2;
                subscription.HasReporting = true;
                subscription.HasAdvancedReporting = true;
                subscription.HasInvoicing = true;
                break;
            default:
                // Trial defaults
                subscription.MaxUsers = 3;
                subscription.MaxProducts = 50;
                subscription.MaxOrders = 100;
                subscription.MaxWarehouses = 1;
                subscription.HasReporting = true;
                subscription.HasAdvancedReporting = false;
                subscription.HasInvoicing = false;
                break;
        }
    }
}