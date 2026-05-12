using System.Net.Http.Json;
using System.Text.Json;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Application.Jobs;
using EWallet.Modules.Merchants.Domain.Entities;
using EWallet.Modules.Merchants.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 0)]
public sealed class DispatchWebhookJob(
    MerchantsDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IWebhookSigner webhookSigner,
    IHttpClientFactory httpClientFactory,
    IBackgroundJobClient backgroundJobClient,
    ILogger<DispatchWebhookJob> logger)
    : IDispatchWebhookJob
{
    private const string ProtectorPurpose = "Merchants.WebhookSecret";

    public async Task RunAsync(Guid paymentRequestId, int attemptNumber)
    {
        var request = await dbContext.PaymentRequests
            .FirstOrDefaultAsync(r => r.Id == paymentRequestId);

        if (request is null)
        {
            logger.LogWarning("DispatchWebhookJob: PaymentRequest {Id} not found", paymentRequestId);
            return;
        }

        var merchant = await dbContext.Merchants
            .FirstOrDefaultAsync(m => m.Id == request.MerchantId);

        if (merchant is null)
        {
            logger.LogWarning("DispatchWebhookJob: Merchant {Id} not found", request.MerchantId);
            return;
        }

        var delivery = WebhookDelivery.Create(paymentRequestId, merchant.Id, attemptNumber);
        dbContext.WebhookDeliveries.Add(delivery);

        var now = DateTimeOffset.UtcNow;
        var payload = JsonSerializer.Serialize(new
        {
            eventType = $"payment_request.{request.Status.ToString().ToLowerInvariant()}",
            paymentRequestId = request.Id,
            merchantId = request.MerchantId,
            amount = request.Amount,
            currency = request.Currency,
            customerPhoneNumber = request.CustomerPhoneNumber,
            resolvedAt = request.ResolvedAt,
            status = request.Status.ToString()
        });

        string signature;
        try
        {
            var protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
            var secretBytes = protector.Unprotect(merchant.WebhookSecretEncrypted!);
            signature = webhookSigner.Sign(payload, secretBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decrypt webhook secret for merchant {MerchantId}", merchant.Id);
            delivery.MarkPermanentlyFailed();
            await dbContext.SaveChangesAsync();
            return;
        }

        int? httpStatus = null;
        string? errorMessage = null;
        bool delivered = false;

        try
        {
            var httpClient = httpClientFactory.CreateClient("webhook");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, merchant.CallbackUrl);
            httpRequest.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            httpRequest.Headers.TryAddWithoutValidation("X-Webhook-Signature", signature);
            httpRequest.Headers.TryAddWithoutValidation("X-Webhook-Timestamp", now.ToString("O"));

            var response = await httpClient.SendAsync(httpRequest, cts.Token);
            httpStatus = (int)response.StatusCode;
            delivered = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            logger.LogWarning(ex,
                "Webhook HTTP call failed for PaymentRequest {RequestId} attempt {Attempt}",
                paymentRequestId, attemptNumber);
        }

        if (delivered)
        {
            delivery.MarkDelivered(httpStatus!.Value);
            logger.LogInformation(
                "Webhook delivered for PaymentRequest {RequestId} attempt {Attempt} — HTTP {Status}",
                paymentRequestId, attemptNumber, httpStatus);
        }
        else if (attemptNumber < 10)
        {
            var nextRetryAt = now.AddMinutes(2);
            delivery.MarkFailed(httpStatus, errorMessage, nextRetryAt);

            backgroundJobClient.Schedule<IDispatchWebhookJob>(
                j => j.RunAsync(paymentRequestId, attemptNumber + 1),
                TimeSpan.FromMinutes(2));

            logger.LogWarning(
                "Webhook failed for PaymentRequest {RequestId} attempt {Attempt}/{Max} — scheduling retry",
                paymentRequestId, attemptNumber, 10);
        }
        else
        {
            delivery.MarkPermanentlyFailed();
            logger.LogError(
                "Webhook permanently failed for PaymentRequest {RequestId} after {Max} attempts",
                paymentRequestId, 10);
        }

        await dbContext.SaveChangesAsync();
    }
}
