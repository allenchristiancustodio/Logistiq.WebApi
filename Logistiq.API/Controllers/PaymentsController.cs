// Logistiq.API/Controllers/PaymentsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Logistiq.Application.Payments;
using Logistiq.Application.Payments.DTOs;
using Logistiq.Application.Subscriptions.DTOs;

namespace Logistiq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IStripeService stripeService, ILogger<PaymentsController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    [HttpPost("create-checkout-session")]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            // Get organization ID from current user context
            var organizationId = User.FindFirst("org_id")?.Value;
            if (string.IsNullOrEmpty(organizationId))
            {
                return BadRequest(new { error = "Organization context required" });
            }

            // Set organization ID in request
            request.OrganizationId = organizationId;

            var response = await _stripeService.CreateCheckoutSessionAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("create-portal-session")]
    public async Task<ActionResult<CreatePortalSessionResponse>> CreatePortalSession([FromBody] CreatePortalSessionRequest request)
    {
        try
        {
            var response = await _stripeService.CreatePortalSessionAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create portal session");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("create-customer")]
    public async Task<ActionResult<CreateCustomerResponse>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            // Get organization context
            var organizationId = User.FindFirst("org_id")?.Value;
            if (string.IsNullOrEmpty(organizationId))
            {
                return BadRequest(new { error = "Organization context required" });
            }

            request.OrganizationId = organizationId;

            var response = await _stripeService.CreateCustomerAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("prices")]
    public async Task<ActionResult<List<StripePriceResponse>>> GetPrices()
    {
        try
        {
            var prices = await _stripeService.GetProductPricesAsync();
            return Ok(prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get prices");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("subscription/{subscriptionId}")]
    public async Task<ActionResult<StripeSubscriptionResponse>> GetSubscription(string subscriptionId)
    {
        try
        {
            var subscription = await _stripeService.GetSubscriptionAsync(subscriptionId);
            if (subscription == null)
                return NotFound();

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription {SubscriptionId}", subscriptionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("subscription/{subscriptionId}/cancel")]
    public async Task<ActionResult<StripeSubscriptionResponse>> CancelSubscription(
        string subscriptionId,
        [FromBody] CancelStripeSubscriptionRequest request)
    {
        try
        {
            var subscription = await _stripeService.CancelSubscriptionAsync(subscriptionId, request.Immediately);
            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", subscriptionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("subscription/{subscriptionId}")]
    public async Task<ActionResult<StripeSubscriptionResponse>> UpdateSubscription(
        string subscriptionId,
        [FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            var subscription = await _stripeService.UpdateSubscriptionAsync(subscriptionId, request);
            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subscription {SubscriptionId}", subscriptionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    public async Task<ActionResult<WebhookEventResponse>> HandleStripeWebhook()
    {
        try
        {
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                return BadRequest("Missing Stripe signature");
            }

            var response = await _stripeService.HandleWebhookAsync(body, signature);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid webhook signature");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Stripe webhook");
            return StatusCode(500, new { error = "Failed to process webhook" });
        }
    }
}

public class CancelStripeSubscriptionRequest
{
    public bool Immediately { get; set; } = false;
}