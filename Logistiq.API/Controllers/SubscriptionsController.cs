// Logistiq.API/Controllers/SubscriptionsController.cs - Updated to use PlanManagementService
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Subscriptions;
using Logistiq.Application.Subscriptions.DTOs;

namespace Logistiq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPlanManagementService _planManagementService;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        IPlanManagementService planManagementService)
    {
        _subscriptionService = subscriptionService;
        _planManagementService = planManagementService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionResponse>> GetCurrentSubscription()
    {
        try
        {
            var subscription = await _subscriptionService.GetCurrentSubscriptionAsync();
            if (subscription == null)
                return NotFound("No subscription found for current organization");

            return Ok(subscription);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanResponse>>> GetAvailablePlans()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();
        return Ok(plans);
    }

    [HttpGet("limits")]
    public async Task<ActionResult<SubscriptionLimitsResponse>> GetSubscriptionLimits()
    {
        try
        {
            var limits = await _subscriptionService.GetSubscriptionLimitsAsync();
            return Ok(limits);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("usage")]
    public async Task<ActionResult<SubscriptionUsageResponse>> GetUsageStats()
    {
        try
        {
            var usage = await _subscriptionService.GetUsageStatsAsync();
            return Ok(usage);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("trial")]
    public async Task<ActionResult<SubscriptionResponse>> CreateTrialSubscription([FromBody] CreateTrialSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.CreateTrialSubscriptionAsync(request);
            return CreatedAtAction(nameof(GetCurrentSubscription), subscription);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("paid")]
    public async Task<ActionResult<SubscriptionResponse>> CreatePaidSubscription([FromBody] CreatePaidSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.CreatePaidSubscriptionAsync(request);
            return CreatedAtAction(nameof(GetCurrentSubscription), subscription);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SubscriptionResponse>> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.UpdateSubscriptionAsync(id, request);
            return Ok(subscription);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelSubscription(Guid id, [FromBody] CancelSubscriptionRequest request)
    {
        try
        {
            await _subscriptionService.CancelSubscriptionAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/reactivate")]
    public async Task<ActionResult<SubscriptionResponse>> ReactivateSubscription(Guid id)
    {
        try
        {
            var subscription = await _subscriptionService.ReactivateSubscriptionAsync(id);
            return Ok(subscription);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("check-limit")]
    public async Task<ActionResult<bool>> CheckLimit([FromBody] CheckLimitRequest request)
    {
        try
        {
            var canAdd = await _subscriptionService.CheckLimitAsync(request.LimitType, request.CurrentCount);
            return Ok(new { canAdd, limitType = request.LimitType.ToString() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Plan Management Endpoints (using IPlanManagementService)
    [HttpPost("change-plan")]
    public async Task<ActionResult<SubscriptionResponse>> ChangePlan([FromBody] ChangePlanRequest request)
    {
        try
        {
            var subscription = await _planManagementService.ChangePlanAsync(request);
            return Ok(subscription);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Subscription not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("upgrade-to-pro")]
    public async Task<ActionResult<SubscriptionResponse>> UpgradeToPro([FromBody] UpgradeToProRequest request)
    {
        try
        {
            var subscription = await _planManagementService.UpgradeToProAsync(request.IsAnnual);
            return Ok(subscription);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("downgrade-to-starter")]
    public async Task<ActionResult<SubscriptionResponse>> DowngradeToStarter()
    {
        try
        {
            var subscription = await _planManagementService.DowngradeToStarterAsync();
            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("can-change-to/{planId}")]
    public async Task<ActionResult<bool>> CanChangeToPlan(string planId)
    {
        try
        {
            var canChange = await _planManagementService.CanChangeToPlanAsync(planId);
            return Ok(new { canChange, planId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("upgrade-recommendations")]
    public async Task<ActionResult<List<string>>> GetUpgradeRecommendations()
    {
        try
        {
            var recommendations = await _planManagementService.GetUpgradeRecommendationsAsync();
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class CheckLimitRequest
{
    public SubscriptionLimitType LimitType { get; set; }
    public int CurrentCount { get; set; }
}

public class UpgradeToProRequest
{
    public bool IsAnnual { get; set; } = false;
}