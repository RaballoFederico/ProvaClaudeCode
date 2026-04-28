using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;

namespace FilmAPI.Endpoints;

public static class StripeWebhookEndpoints
{
    public static IEndpointRouteBuilder MapStripeWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/stripe", [EnableRateLimiting("webhook")] async (
            HttpContext context,
            IConfiguration configuration,
            IPagamentoService pagamentoService,
            ILoggerFactory loggerFactory) =>
        {
            string payload;
            using (var reader = new StreamReader(context.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            var signature = context.Request.Headers["Stripe-Signature"].FirstOrDefault();
            var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                ?? configuration["Stripe:WebhookSecret"];

            var result = await pagamentoService.GestisciWebhookAsync(payload, signature, webhookSecret);

            var logger = loggerFactory.CreateLogger("StripeWebhook");
            if (!result.Success)
            {
                logger.LogWarning("Stripe webhook rejected: {Message}", result.Message);
                return Results.BadRequest(new
                {
                    message = result.Message,
                    eventId = result.EventId,
                    eventType = result.EventType
                });
            }

            logger.LogInformation(
                "Stripe webhook processed: event={EventType} id={EventId} session={Session} user={UserId} amount={Amount} alreadyProcessed={AlreadyProcessed}",
                result.EventType,
                result.EventId,
                result.CheckoutSessionId,
                result.UserId,
                result.Amount,
                result.AlreadyProcessed);

            return Results.Ok(new
            {
                message = result.Message,
                eventId = result.EventId,
                eventType = result.EventType,
                checkoutSessionId = result.CheckoutSessionId,
                alreadyProcessed = result.AlreadyProcessed
            });
        })
        .AllowAnonymous();

        return app;
    }
}
