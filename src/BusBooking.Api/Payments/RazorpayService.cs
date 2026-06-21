using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace BusBooking.Api.Payments;

public sealed class RazorpayService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    private readonly string _keyId = configuration["Razorpay:KeyId"]
        ?? throw new InvalidOperationException("Razorpay:KeyId is not configured.");
    private readonly string _keySecret = configuration["Razorpay:KeySecret"]
        ?? throw new InvalidOperationException("Razorpay:KeySecret is not configured.");

    public async Task<RazorpayOrderResult> CreateOrderAsync(
        decimal amount, string receipt, CancellationToken ct = default)
    {
        var amountPaise = (long)(amount * 100);
        var client = httpClientFactory.CreateClient("Razorpay");

        var response = await client.PostAsJsonAsync("orders", new
        {
            amount = amountPaise,
            currency = "INR",
            receipt,
        }, ct);

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<RazorpayOrderResponse>(cancellationToken: ct);
        return new RazorpayOrderResult(data!.Id, amountPaise, "INR", _keyId);
    }

    public bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
    {
        var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
        var keyBytes = Encoding.UTF8.GetBytes(_keySecret);
        var msgBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        return computed == razorpaySignature;
    }

    private sealed record RazorpayOrderResponse(string Id);
}

public sealed record RazorpayOrderResult(
    string OrderId, long AmountPaise, string Currency, string KeyId);
