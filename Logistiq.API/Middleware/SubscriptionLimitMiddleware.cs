// Logistiq.API/Middleware/SubscriptionLimitMiddleware.cs
using System.Text.Json;
using Logistiq.Application.Subscriptions;
using Logistiq.Application.Subscriptions.DTOs;

namespace Logistiq.API.Middleware;

public class SubscriptionLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionLimitMiddleware> _logger;

    public SubscriptionLimitMiddleware(RequestDelegate next, ILogger<SubscriptionLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService)
    {
        // Skip for non-authenticated requests
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Skip for GET requests and subscription endpoints
        if (context.Request.Method == "GET" ||
            context.Request.Path.StartsWithSegments("/api/subscriptions") ||
            context.Request.Path.StartsWithSegments("/api/test"))
        {
            await _next(context);
            return;
        }

        try
        {
            var limitType = GetLimitTypeFromPath(context.Request.Path);
            if (limitType.HasValue)
            {
                var canProceed = await CheckSubscriptionLimit(subscriptionService, limitType.Value);
                if (!canProceed)
                {
                    await HandleLimitExceeded(context, limitType.Value);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking subscription limits, allowing request to proceed");
        }

        await _next(context);
    }

    private static SubscriptionLimitType? GetLimitTypeFromPath(PathString path)
    {
        if (path.StartsWithSegments("/api/products") && !path.Value!.Contains("check-sku"))
            return SubscriptionLimitType.Products;

        if (path.StartsWithSegments("/api/warehouses"))
            return SubscriptionLimitType.Warehouses;

        if (path.StartsWithSegments("/api/orders"))
            return SubscriptionLimitType.Orders;

        if (path.StartsWithSegments("/api/users"))
            return SubscriptionLimitType.Users;

        return null;
    }

    private async Task<bool> CheckSubscriptionLimit(ISubscriptionService subscriptionService, SubscriptionLimitType limitType)
    {
        try
        {
            var usage = await subscriptionService.GetUsageStatsAsync();
            var metric = limitType.ToString();

            if (usage.UsageMetrics.TryGetValue(metric, out var usageMetric))
            {
                return !usageMetric.IsAtLimit;
            }

            return true; // Allow if we can't determine usage
        }
        catch
        {
            return true; // Allow on error
        }
    }

    private async Task HandleLimitExceeded(HttpContext context, SubscriptionLimitType limitType)
    {
        context.Response.StatusCode = 402; // Payment Required
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Subscription limit exceeded",
            limitType = limitType.ToString(),
            message = $"You have reached your subscription limit for {limitType.ToString().ToLower()}. Please upgrade your plan to continue.",
            upgradeUrl = "/billing/upgrade"
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}