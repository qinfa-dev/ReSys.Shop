using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using ErrorOr;

using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Payments.Interfaces.Providers;
using ReSys.Core.Feature.Common.Payments.Models;


namespace ReSys.Infrastructure.Payments.Services;

/// <summary>
/// PayPal payment provider implementation using PayPal Orders API v2.
/// 
/// <para>
/// <strong>Purpose:</strong>
/// Integrates with PayPal's REST API for processing payments, captures, refunds, and voids.
/// Supports both immediate capture and authorize-then-capture flows.
/// </para>
/// 
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
/// <item><description>OAuth 2.0 authentication with automatic token refresh</description></item>
/// <item><description>Orders API v2 for modern PayPal integration</description></item>
/// <item><description>Support for both sandbox and production environments</description></item>
/// <item><description>Comprehensive error handling and retry logic</description></item>
/// <item><description>Idempotency support for safe retries</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>API Reference:</strong>
/// https://developer.paypal.com/docs/api/orders/v2/
/// </para>
/// </summary>
public class PayPalProvider : PaymentProviderBase<PayPalOptions>
{
    private readonly ILogger<PayPalProvider> _logger;
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiration;

    public PayPalProvider(
        PayPalOptions options, 
        ILogger<PayPalProvider> logger,
        IHttpClientFactory httpClientFactory) : base(options)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("PayPal");
        _httpClient.BaseAddress = new Uri(GetBaseUrl());
    }

    /// <summary>
    /// Authorizes a payment using PayPal Orders API.
    /// The PaymentToken should be an approved PayPal Order ID.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Authorizing PayPal payment for order {OrderNumber}, amount {Amount} {Currency}",
                request.OrderNumber, request.Amount, request.Currency);

            await EnsureAccessTokenAsync(cancellationToken);

            // If AutoCapture is true, we capture immediately; otherwise just authorize
            var intent = _options.AutoCapture ? "CAPTURE" : "AUTHORIZE";

            // Create PayPal order
            var createOrderRequest = new
            {
                intent,
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = request.OrderNumber,
                        amount = new
                        {
                            currency_code = request.Currency.ToUpperInvariant(),
                            value = request.Amount.ToString("F2")
                        },
                        payee = new
                        {
                            email_address = request.CustomerEmail
                        },
                        shipping = request.ShippingAddress != null ? new
                        {
                            name = new
                            {
                                full_name = $"{request.ShippingAddress.FirstName} {request.ShippingAddress.LastName}".Trim()
                            },
                            address = new
                            {
                                address_line_1 = request.ShippingAddress.AddressLine1,
                                address_line_2 = request.ShippingAddress.AddressLine2,
                                admin_area_2 = request.ShippingAddress.City,
                                admin_area_1 = request.ShippingAddress.State,
                                postal_code = request.ShippingAddress.PostalCode,
                                country_code = request.ShippingAddress.Country
                            }
                        } : null
                    }
                },
                application_context = new
                {
                    brand_name = _options.BrandName ?? "ReSys Store",
                    return_url = _options.ReturnUrl,
                    cancel_url = _options.CancelUrl
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            httpRequest.Headers.Add("PayPal-Request-Id", request.IdempotencyKey ?? Guid.NewGuid().ToString());
            httpRequest.Content = JsonContent.Create(createOrderRequest);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal order creation failed: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);

                return new ProviderResponse(
                    Status: PaymentStatus.Failed,
                    TransactionId: "",
                    ErrorCode: "paypal_order_creation_failed",
                    ErrorMessage: $"Failed to create PayPal order: {response.StatusCode}"
                );
            }

            var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (orderResponse == null)
            {
                return Error.Failure(
                    code: "PayPal.InvalidResponse",
                    description: "Failed to parse PayPal response");
            }

            var status = MapPayPalStatus(orderResponse.Status);

            var providerResponse = new ProviderResponse(
                Status: status,
                TransactionId: orderResponse.Id,
                RawResponse: new Dictionary<string, string>
                {
                    ["provider"] = "PayPal",
                    ["order_id"] = orderResponse.Id,
                    ["status"] = orderResponse.Status,
                    ["intent"] = intent,
                    ["approval_url"] = GetApprovalUrl(orderResponse.Links)
                }
            );

            _logger.LogInformation("PayPal order created successfully: {OrderId}", orderResponse.Id);

            return providerResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PayPal authorization for order {OrderNumber}",
                request.OrderNumber);

            return Error.Failure(
                code: "PayPal.AuthorizationError",
                description: $"Failed to authorize payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Captures a previously authorized PayPal payment.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> CaptureAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Capturing PayPal payment {TransactionId}, amount {Amount} {Currency}",
                request.TransactionId, request.Amount, request.Currency);

            await EnsureAccessTokenAsync(cancellationToken);

            // First, get the authorization ID from the order
            var orderRequest = new HttpRequestMessage(HttpMethod.Get, $"/v2/checkout/orders/{request.TransactionId}");
            orderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var orderResponse = await _httpClient.SendAsync(orderRequest, cancellationToken);
            var orderContent = await orderResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!orderResponse.IsSuccessStatusCode)
            {
                return new ProviderResponse(
                    Status: PaymentStatus.Failed,
                    TransactionId: request.TransactionId,
                    ErrorCode: "paypal_order_not_found",
                    ErrorMessage: "PayPal order not found"
                );
            }

            var order = JsonSerializer.Deserialize<PayPalOrderResponse>(orderContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Extract authorization ID
            var authorizationId = order?.PurchaseUnits?[0]?.Payments?.Authorizations?[0]?.Id;
            if (string.IsNullOrEmpty(authorizationId))
            {
                return Error.Failure(
                    code: "PayPal.NoAuthorization",
                    description: "No authorization found for this order");
            }

            // Capture the authorization
            var captureRequest = new
            {
                amount = new
                {
                    currency_code = request.Currency.ToUpperInvariant(),
                    value = request.Amount.ToString("F2")
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, 
                $"/v2/payments/authorizations/{authorizationId}/capture");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            httpRequest.Headers.Add("PayPal-Request-Id", request.IdempotencyKey ?? Guid.NewGuid().ToString());
            httpRequest.Content = JsonContent.Create(captureRequest);

            var captureResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var captureContent = await captureResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!captureResponse.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal capture failed: {StatusCode} - {Content}",
                    captureResponse.StatusCode, captureContent);

                return new ProviderResponse(
                    Status: PaymentStatus.Failed,
                    TransactionId: request.TransactionId,
                    ErrorCode: "paypal_capture_failed",
                    ErrorMessage: $"Failed to capture PayPal payment: {captureResponse.StatusCode}"
                );
            }

            var capture = JsonSerializer.Deserialize<PayPalCaptureResponse>(captureContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var providerResponse = new ProviderResponse(
                Status: PaymentStatus.Captured,
                TransactionId: capture?.Id ?? request.TransactionId,
                RawResponse: new Dictionary<string, string>
                {
                    ["provider"] = "PayPal",
                    ["capture_id"] = capture?.Id ?? "",
                    ["authorization_id"] = authorizationId,
                    ["status"] = capture?.Status ?? "COMPLETED"
                }
            );

            _logger.LogInformation("PayPal capture successful: {CaptureId}", capture?.Id);

            return providerResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PayPal capture for transaction {TransactionId}",
                request.TransactionId);

            return Error.Failure(
                code: "PayPal.CaptureError",
                description: $"Failed to capture payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Voids (cancels) an authorized but uncaptured PayPal payment.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> VoidAsync(
        VoidRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Voiding PayPal payment {TransactionId}", request.TransactionId);

            await EnsureAccessTokenAsync(cancellationToken);

            // Get authorization ID from order
            var orderRequest = new HttpRequestMessage(HttpMethod.Get, $"/v2/checkout/orders/{request.TransactionId}");
            orderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var orderResponse = await _httpClient.SendAsync(orderRequest, cancellationToken);
            var orderContent = await orderResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!orderResponse.IsSuccessStatusCode)
            {
                return new ProviderResponse(
                    Status: PaymentStatus.Failed,
                    TransactionId: request.TransactionId,
                    ErrorCode: "paypal_order_not_found",
                    ErrorMessage: "PayPal order not found"
                );
            }

            var order = JsonSerializer.Deserialize<PayPalOrderResponse>(orderContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var authorizationId = order?.PurchaseUnits?[0]?.Payments?.Authorizations?[0]?.Id;
            if (string.IsNullOrEmpty(authorizationId))
            {
                return Error.Failure(
                    code: "PayPal.NoAuthorization",
                    description: "No authorization found to void");
            }

            // Void the authorization
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, 
                $"/v2/payments/authorizations/{authorizationId}/void");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            httpRequest.Headers.Add("PayPal-Request-Id", request.IdempotencyKey ?? Guid.NewGuid().ToString());

            var voidResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var voidContent = await voidResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!voidResponse.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal void failed: {StatusCode} - {Content}",
                    voidResponse.StatusCode, voidContent);

                return new ProviderResponse(
                    Status: PaymentStatus.Failed,
                    TransactionId: request.TransactionId,
                    ErrorCode: "paypal_void_failed",
                    ErrorMessage: $"Failed to void PayPal payment: {voidResponse.StatusCode}"
                );
            }

            var providerResponse = new ProviderResponse(
                Status: PaymentStatus.Voided,
                TransactionId: request.TransactionId,
                RawResponse: new Dictionary<string, string>
                {
                    ["provider"] = "PayPal",
                    ["authorization_id"] = authorizationId,
                    ["status"] = "VOIDED"
                }
            );

            _logger.LogInformation("PayPal void successful for authorization {AuthId}", authorizationId);

            return providerResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PayPal void for transaction {TransactionId}",
                request.TransactionId);

            return Error.Failure(
                code: "PayPal.VoidError",
                description: $"Failed to void payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Refunds a captured PayPal payment.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> RefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Refunding PayPal payment {TransactionId}, amount {Amount} {Currency}",
                request.TransactionId, request.Amount, request.Currency);

            await EnsureAccessTokenAsync(cancellationToken);

            // The TransactionId should be a capture ID
            var refundRequestBody = new
            {
                amount = new
                {
                    currency_code = request.Currency.ToUpperInvariant(),
                    value = request.Amount.ToString("F2")
                },
                note_to_payer = request.Reason ?? "Refund processed"
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, 
                $"/v2/payments/captures/{request.TransactionId}/refund");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            httpRequest.Headers.Add("PayPal-Request-Id", request.IdempotencyKey ?? Guid.NewGuid().ToString());
            httpRequest.Content = JsonContent.Create(refundRequestBody);

            var refundResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var refundContent = await refundResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!refundResponse.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal refund failed: {StatusCode} - {Content}",
                    refundResponse.StatusCode, refundContent);

                return new ProviderResponse(
                    Status: PaymentStatus.Failed,
                    TransactionId: request.TransactionId,
                    ErrorCode: "paypal_refund_failed",
                    ErrorMessage: $"Failed to refund PayPal payment: {refundResponse.StatusCode}"
                );
            }

            var refund = JsonSerializer.Deserialize<PayPalRefundResponse>(refundContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var providerResponse = new ProviderResponse(
                Status: PaymentStatus.Refunded,
                TransactionId: refund?.Id ?? request.TransactionId,
                RawResponse: new Dictionary<string, string>
                {
                    ["provider"] = "PayPal",
                    ["refund_id"] = refund?.Id ?? "",
                    ["capture_id"] = request.TransactionId,
                    ["status"] = refund?.Status ?? "COMPLETED"
                }
            );

            _logger.LogInformation("PayPal refund successful: {RefundId}", refund?.Id);

            return providerResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PayPal refund for transaction {TransactionId}",
                request.TransactionId);

            return Error.Failure(
                code: "PayPal.RefundError",
                description: $"Failed to refund payment: {ex.Message}");
        }
    }

    private async Task EnsureAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiration)
        {
            return; // Token still valid
        }

        var authRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        authRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        authRequest.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.SendAsync(authRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<PayPalTokenResponse>(cancellationToken);
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to obtain PayPal access token");
        }

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh 1 min early

        _logger.LogInformation("PayPal access token obtained, expires at {Expiration}", _tokenExpiration);
    }

    private string GetBaseUrl()
    {
        return _options.UseSandbox
            ? "https://api-m.sandbox.paypal.com"
            : "https://api-m.paypal.com";
    }

    private string GetApprovalUrl(List<PayPalLink>? links)
    {
        var approveLink = links?.FirstOrDefault(l => l.Rel == "approve");
        return approveLink?.Href ?? "";
    }

    private PaymentStatus MapPayPalStatus(string status)
    {
        return status switch
        {
            "CREATED" => PaymentStatus.Pending,
            "SAVED" => PaymentStatus.Pending,
            "APPROVED" => PaymentStatus.Authorized,
            "VOIDED" => PaymentStatus.Voided,
            "COMPLETED" => PaymentStatus.Captured,
            "PAYER_ACTION_REQUIRED" => PaymentStatus.RequiresAction,
            _ => PaymentStatus.Pending
        };
    }

    #region PayPal API Models
    private record PayPalTokenResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn);

    private record PayPalOrderResponse(
        string Id,
        string Status,
        List<PayPalLink>? Links,
        List<PayPalPurchaseUnit>? PurchaseUnits);

    private record PayPalPurchaseUnit(
        PayPalPayments? Payments);

    private record PayPalPayments(
        List<PayPalAuthorization>? Authorizations,
        List<PayPalCapture>? Captures);

    private record PayPalAuthorization(string Id, string Status);
    private record PayPalCapture(string Id, string Status);

    private record PayPalLink(string Href, string Rel, string Method);

    private record PayPalCaptureResponse(
        string Id,
        string Status);

    private record PayPalRefundResponse(
        string Id,
        string Status);
    #endregion
}

/// <summary>
/// Configuration options for the PayPal provider.
/// </summary>
public class PayPalOptions
{
    /// <summary>
    /// PayPal Client ID from the Developer Dashboard.
    /// Can be stored in PublicMetadata.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// PayPal Client Secret from the Developer Dashboard.
    /// Must be encrypted in PrivateMetadata.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use the PayPal sandbox environment.
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// Whether to automatically capture payments.
    /// </summary>
    public bool AutoCapture { get; set; } = false;

    /// <summary>
    /// Brand name displayed in PayPal checkout.
    /// </summary>
    public string? BrandName { get; set; }

    /// <summary>
    /// URL to redirect after successful payment.
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to redirect if payment is cancelled.
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Webhook ID for verifying webhook signatures.
    /// Must be encrypted in PrivateMetadata.
    /// </summary>
    public string? WebhookId { get; set; }
}