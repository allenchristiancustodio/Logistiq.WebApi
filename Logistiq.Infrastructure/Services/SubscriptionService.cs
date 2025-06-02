using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Subscriptions;
using Logistiq.Application.Subscriptions.DTOs;
using Logistiq.Domain.Entities;
using Logistiq.Domain.Enums;
using Logistiq.Persistence.Data;
using Logistiq.Application.Payments;

namespace Logistiq.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly LogistiqDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IStripeService _stripeService;

    public SubscriptionService(
        LogistiqDbContext context,
        ICurrentUserService currentUser,
        ILogger<SubscriptionService> logger,
        IStripeService stripeService)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
        _stripeService = stripeService;
    }

    public async Task<SubscriptionResponse?> GetCurrentSubscriptionAsync()
    {
        var organizationId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(organizationId))
            return null;

        var subscription = await _context.Subscriptions
            .Include(s => s.Organization)
            .FirstOrDefaultAsync(s => s.ClerkOrganizationId == organizationId);

        if (subscription == null)
        {
            // Auto-create trial subscription for new organizations
            _logger.LogInformation("No subscription found for organization {OrganizationId}, creating trial", organizationId);
            return await CreateTrialSubscriptionAsync(new CreateTrialSubscriptionRequest());
        }

        return await MapToResponseWithUsage(subscription);
    }

    public async Task<SubscriptionResponse> CreateTrialSubscriptionAsync(CreateTrialSubscriptionRequest request)
    {
        var organizationId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(organizationId))
            throw new UnauthorizedAccessException("No organization context found");

        // Check if subscription already exists
        var existingSubscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.ClerkOrganizationId == organizationId);

        if (existingSubscription != null)
            throw new InvalidOperationException("Organization already has a subscription");

        var trialEndDate = DateTime.UtcNow.AddDays(request.TrialDays);

        var subscription = new Subscription
        {
            ClerkOrganizationId = organizationId,
            PlanName = request.PlanName,
            MonthlyPrice = 0,
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow,
            EndDate = trialEndDate,
            TrialEndDate = trialEndDate,

            // Trial plan limits
            MaxUsers = 3,
            MaxProducts = 50,
            MaxOrders = 100,
            MaxWarehouses = 1,
            HasAdvancedReporting = false,
            HasReporting = true,
            HasInvoicing = false
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created trial subscription for organization {OrganizationId}", organizationId);

        return await MapToResponseWithUsage(subscription);
    }

    public async Task<SubscriptionResponse> CreatePaidSubscriptionAsync(CreatePaidSubscriptionRequest request)
    {
        var organizationId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(organizationId))
            throw new UnauthorizedAccessException("No organization context found");

        var existingSubscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.ClerkOrganizationId == organizationId);

        if (existingSubscription != null)
        {
            // Update existing subscription to paid
            existingSubscription.StripeCustomerId = request.StripeCustomerId;
            existingSubscription.StripeSubscriptionId = request.StripeSubscriptionId;
            existingSubscription.StripePriceId = request.StripePriceId;
            existingSubscription.PlanName = request.PlanName;
            existingSubscription.MonthlyPrice = request.MonthlyPrice;
            existingSubscription.Status = SubscriptionStatus.Active;
            existingSubscription.TrialEndDate = request.TrialEndDate;

            // Update limits based on plan
            SetPlanLimits(existingSubscription, request.PlanName);
        }
        else
        {
            // Create new paid subscription
            var subscription = new Subscription
            {
                ClerkOrganizationId = organizationId,
                StripeCustomerId = request.StripeCustomerId,
                StripeSubscriptionId = request.StripeSubscriptionId,
                StripePriceId = request.StripePriceId,
                PlanName = request.PlanName,
                MonthlyPrice = request.MonthlyPrice,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                TrialEndDate = request.TrialEndDate
            };

            SetPlanLimits(subscription, request.PlanName);
            _context.Subscriptions.Add(subscription);
            existingSubscription = subscription;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created/updated paid subscription for organization {OrganizationId}, plan: {PlanName}",
            organizationId, request.PlanName);

        return await MapToResponseWithUsage(existingSubscription);
    }

    public async Task<SubscriptionResponse> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequest request)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription == null)
            throw new KeyNotFoundException($"Subscription with ID {id} not found");

        // Update provided fields
        if (request.PlanName != null) subscription.PlanName = request.PlanName;
        if (request.MonthlyPrice.HasValue) subscription.MonthlyPrice = request.MonthlyPrice.Value;
        if (request.Status.HasValue) subscription.Status = request.Status.Value;
        if (request.EndDate.HasValue) subscription.EndDate = request.EndDate.Value;
        if (request.MaxUsers.HasValue) subscription.MaxUsers = request.MaxUsers.Value;
        if (request.MaxProducts.HasValue) subscription.MaxProducts = request.MaxProducts.Value;
        if (request.MaxOrders.HasValue) subscription.MaxOrders = request.MaxOrders.Value;
        if (request.MaxWarehouses.HasValue) subscription.MaxWarehouses = request.MaxWarehouses.Value;
        if (request.HasAdvancedReporting.HasValue) subscription.HasAdvancedReporting = request.HasAdvancedReporting.Value;
        if (request.HasReporting.HasValue) subscription.HasReporting = request.HasReporting.Value;
        if (request.HasInvoicing.HasValue) subscription.HasInvoicing = request.HasInvoicing.Value;

        await _context.SaveChangesAsync();

        return await MapToResponseWithUsage(subscription);
    }

    public async Task CancelSubscriptionAsync(Guid id, CancelSubscriptionRequest request)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription == null)
            throw new KeyNotFoundException($"Subscription with ID {id} not found");

        if (request.CancelImmediately)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.EndDate = DateTime.UtcNow;
        }
        else
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            // Keep current end date for end-of-period cancellation
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled subscription {SubscriptionId} for organization {OrganizationId}, immediate: {Immediate}",
            id, subscription.ClerkOrganizationId, request.CancelImmediately);
    }

    public async Task<SubscriptionResponse> ReactivateSubscriptionAsync(Guid id)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription == null)
            throw new KeyNotFoundException($"Subscription with ID {id} not found");

        subscription.Status = SubscriptionStatus.Active;
        subscription.EndDate = DateTime.UtcNow.AddMonths(1);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reactivated subscription {SubscriptionId} for organization {OrganizationId}",
            id, subscription.ClerkOrganizationId);

        return await MapToResponseWithUsage(subscription);
    }

    public async Task<List<SubscriptionPlanResponse>> GetAvailablePlansAsync()
    {
        // In a real app, these would come from a database or Stripe
        return new List<SubscriptionPlanResponse>
        {
            new()
            {
                Id = "starter",
                Name = "Starter",
                Description = "Perfect for small businesses getting started",
                MonthlyPrice = 29,
                AnnualPrice = 290,
                MaxUsers = 5,
                MaxProducts = 500,
                MaxOrders = 1000,
                MaxWarehouses = 2,
                HasReporting = true,
                HasAdvancedReporting = false,
                HasInvoicing = false,
                Features = new() { "Up to 5 users", "500 products", "1K orders/month", "Basic reporting", "Email support" },
                StripePriceIdMonthly = "price_starter_monthly",
                StripePriceIdAnnual = "price_starter_annual"
            },
            new()
            {
                Id = "professional",
                Name = "Professional",
                Description = "Advanced features for growing businesses",
                MonthlyPrice = 79,
                AnnualPrice = 790,
                IsPopular = true,
                MaxUsers = 15,
                MaxProducts = 2000,
                MaxOrders = 5000,
                MaxWarehouses = 5,
                HasReporting = true,
                HasAdvancedReporting = true,
                HasInvoicing = true,
                Features = new() { "Up to 15 users", "2K products", "5K orders/month", "Advanced reporting", "Invoicing", "Priority support" },
                StripePriceIdMonthly = "price_professional_monthly",
                StripePriceIdAnnual = "price_professional_annual"
            },
            new()
            {
                Id = "enterprise",
                Name = "Enterprise",
                Description = "Unlimited everything for large organizations",
                MonthlyPrice = 199,
                AnnualPrice = 1990,
                MaxUsers = int.MaxValue,
                MaxProducts = int.MaxValue,
                MaxOrders = int.MaxValue,
                MaxWarehouses = int.MaxValue,
                HasReporting = true,
                HasAdvancedReporting = true,
                HasInvoicing = true,
                Features = new() { "Unlimited users", "Unlimited products", "Unlimited orders", "All features", "Dedicated support", "Custom integrations" },
                StripePriceIdMonthly = "price_enterprise_monthly",
                StripePriceIdAnnual = "price_enterprise_annual"
            }
        };
    }

    public async Task<SubscriptionLimitsResponse> GetSubscriptionLimitsAsync()
    {
        var subscription = await GetCurrentSubscriptionAsync();
        if (subscription == null)
        {
            // Return trial limits if no subscription
            return new SubscriptionLimitsResponse
            {
                MaxUsers = 3,
                MaxProducts = 50,
                MaxOrders = 100,
                MaxWarehouses = 1,
                HasReporting = true,
                HasAdvancedReporting = false,
                HasInvoicing = false
            };
        }

        return new SubscriptionLimitsResponse
        {
            MaxUsers = subscription.MaxUsers,
            MaxProducts = subscription.MaxProducts,
            MaxOrders = subscription.MaxOrders,
            MaxWarehouses = subscription.MaxWarehouses,
            HasAdvancedReporting = subscription.HasAdvancedReporting,
            HasReporting = subscription.HasReporting,
            HasInvoicing = subscription.HasInvoicing
        };
    }

    public async Task<bool> CheckLimitAsync(SubscriptionLimitType limitType, int currentCount)
    {
        var limits = await GetSubscriptionLimitsAsync();

        return limitType switch
        {
            SubscriptionLimitType.Users => currentCount < limits.MaxUsers,
            SubscriptionLimitType.Products => currentCount < limits.MaxProducts,
            SubscriptionLimitType.Orders => currentCount < limits.MaxOrders,
            SubscriptionLimitType.Warehouses => currentCount < limits.MaxWarehouses,
            _ => true
        };
    }

    public async Task<SubscriptionUsageResponse> GetUsageStatsAsync()
    {
        var organizationId = _currentUser.OrganizationId;
        if (string.IsNullOrEmpty(organizationId))
            throw new UnauthorizedAccessException("No organization context found");

        var limits = await GetSubscriptionLimitsAsync();

        // Get current usage counts
        var userCount = await _context.ApplicationUsers
            .CountAsync(u => u.CurrentOrganizationId == organizationId);

        var productCount = await _context.Products
            .CountAsync(p => p.ClerkOrganizationId == organizationId);

        var orderCount = await _context.Orders
            .CountAsync(o => o.ClerkOrganizationId == organizationId &&
                           o.CreatedAt.Month == DateTime.UtcNow.Month &&
                           o.CreatedAt.Year == DateTime.UtcNow.Year);

        var warehouseCount = await _context.Warehouses
            .CountAsync(w => w.ClerkOrganizationId == organizationId);

        return new SubscriptionUsageResponse
        {
            CurrentUsers = userCount,
            CurrentProducts = productCount,
            CurrentOrders = orderCount,
            CurrentWarehouses = warehouseCount,
            Limits = limits,
            UsageMetrics = new Dictionary<string, UsageMetric>
            {
                ["Users"] = new() { Current = userCount, Limit = limits.MaxUsers },
                ["Products"] = new() { Current = productCount, Limit = limits.MaxProducts },
                ["Orders"] = new() { Current = orderCount, Limit = limits.MaxOrders },
                ["Warehouses"] = new() { Current = warehouseCount, Limit = limits.MaxWarehouses }
            }
        };
    }
    private void SetPlanLimits(Subscription subscription, string planName)
    {
        switch (planName.ToLower())
        {
            case "starter":
                subscription.MaxUsers = 5;
                subscription.MaxProducts = 500;
                subscription.MaxOrders = 1000;
                subscription.MaxWarehouses = 2;
                subscription.HasReporting = true;
                subscription.HasAdvancedReporting = false;
                subscription.HasInvoicing = false;
                break;
            case "professional":
                subscription.MaxUsers = 15;
                subscription.MaxProducts = 2000;
                subscription.MaxOrders = 5000;
                subscription.MaxWarehouses = 5;
                subscription.HasReporting = true;
                subscription.HasAdvancedReporting = true;
                subscription.HasInvoicing = true;
                break;
            case "enterprise":
                subscription.MaxUsers = int.MaxValue;
                subscription.MaxProducts = int.MaxValue;
                subscription.MaxOrders = int.MaxValue;
                subscription.MaxWarehouses = int.MaxValue;
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

    private async Task<SubscriptionResponse> MapToResponseWithUsage(Subscription subscription)
    {
        var organizationId = subscription.ClerkOrganizationId;

        // Get current usage
        var userCount = await _context.ApplicationUsers
            .CountAsync(u => u.CurrentOrganizationId == organizationId);

        var productCount = await _context.Products
            .CountAsync(p => p.ClerkOrganizationId == organizationId);

        var orderCount = await _context.Orders
            .CountAsync(o => o.ClerkOrganizationId == organizationId &&
                           o.CreatedAt.Month == DateTime.UtcNow.Month &&
                           o.CreatedAt.Year == DateTime.UtcNow.Year);

        var warehouseCount = await _context.Warehouses
            .CountAsync(w => w.ClerkOrganizationId == organizationId);

        var now = DateTime.UtcNow;
        var isTrialActive = subscription.TrialEndDate.HasValue && subscription.TrialEndDate > now;
        var daysRemaining = subscription.EndDate > now ? (int)(subscription.EndDate - now).TotalDays : 0;

        return new SubscriptionResponse
        {
            Id = subscription.Id,
            ClerkOrganizationId = subscription.ClerkOrganizationId,
            StripeCustomerId = subscription.StripeCustomerId,
            StripeSubscriptionId = subscription.StripeSubscriptionId,
            StripePriceId = subscription.StripePriceId,
            PlanName = subscription.PlanName,
            MonthlyPrice = subscription.MonthlyPrice,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            IsTrialActive = isTrialActive,
            DaysRemaining = daysRemaining,
            IsExpired = subscription.EndDate < now,

            // Limits
            MaxUsers = subscription.MaxUsers,
            MaxProducts = subscription.MaxProducts,
            MaxOrders = subscription.MaxOrders,
            MaxWarehouses = subscription.MaxWarehouses,
            HasAdvancedReporting = subscription.HasAdvancedReporting,
            HasReporting = subscription.HasReporting,
            HasInvoicing = subscription.HasInvoicing,

            // Current usage
            CurrentUsers = userCount,
            CurrentProducts = productCount,
            CurrentOrders = orderCount,
            CurrentWarehouses = warehouseCount,

            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }
}