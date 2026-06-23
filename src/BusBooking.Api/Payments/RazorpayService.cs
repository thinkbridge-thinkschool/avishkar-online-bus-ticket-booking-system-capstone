using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using BusBooking.Application.Common;
using BusBooking.Application.Tenants;

namespace BusBooking.Api.Payments;

public sealed class TenantRazorpayService(
    ITenantContext tenantContext,
    ITenantRepository tenantRepo,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
{
    private (string KeyId, string KeySecret)? _credentials;

    private async Task<(string KeyId, string KeySecret)> ResolveAsync(CancellationToken ct)
    {
        if (_credentials is not null)
            return _credentials.Value;

        if (tenantContext.IsResolved)
        {
            var tenant = await tenantRepo.GetByIdAsync(tenantContext.TenantId, ct);
            if (tenant?.RazorpayKeyId is not null && tenant.RazorpayKeySecret is not null)
            {
                _credentials = (tenant.RazorpayKeyId, tenant.RazorpayKeySecret);
                return _credentials.Value;
            }
        }

        var platformKeyId     = configuration["Razorpay:KeyId"];
        var platformKeySecret = configuration["Razorpay:KeySecret"];

        if (string.IsNullOrEmpty(platformKeyId) || string.IsNullOrEmpty(platformKeySecret))
            throw new InvalidOperationException(
                "No Razorpay credentials are configured for this tenant.");

        _credentials = (platformKeyId, platformKeySecret);
        return _credentials.Value;
    }

    public async Task<RazorpayOrderResult> CreateOrderAsync(
        decimal amount, string receipt, CancellationToken ct = default)
    {
        var (keyId, keySecret) = await ResolveAsync(ct);
        var amountPaise = (long)(amount * 100);

        var client     = httpClientFactory.CreateClient("RazorpayBase");
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "orders")
        {
            Content = JsonContent.Create(new { amount = amountPaise, currency = "INR", receipt }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<RazorpayOrderResponse>(cancellationToken: ct);
        return new RazorpayOrderResult(data!.Id, amountPaise, "INR", keyId);
    }

    public async Task<bool> VerifySignatureAsync(
        string razorpayOrderId, string razorpayPaymentId, string razorpaySignature,
        CancellationToken ct = default)
    {
        var (_, keySecret) = await ResolveAsync(ct);
        var payload  = $"{razorpayOrderId}|{razorpayPaymentId}";
        var keyBytes = Encoding.UTF8.GetBytes(keySecret);
        var msgBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        return Convert.ToHexString(hash).ToLowerInvariant() == razorpaySignature;
    }

    private sealed record RazorpayOrderResponse(string Id);
}

public sealed record RazorpayOrderResult(
    string OrderId, long AmountPaise, string Currency, string KeyId);
