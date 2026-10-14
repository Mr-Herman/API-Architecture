using System.Net;
using System.Text;
using EAP.Application.Http;
using EAP.Application.Interfaces;
using EAP.Application.Logging;
using EAP.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAP.Infrastructure.Http;

public sealed class HttpApiClient : IHttpApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpApiClient> _logger;
    private readonly HttpApiOptions _options;

    public HttpApiClient(HttpClient httpClient, IOptions<HttpApiOptions> options, ILogger<HttpApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<HttpApiResponse> SendAsync(HttpApiRequest request, CancellationToken cancellationToken = default)
    {
        var endpoint = ResolveEndpoint(request.EndpointName);
        var method = request.Method ?? ParseMethod(endpoint.DefaultMethod);
        var uri = BuildUri(endpoint.Url, request.QueryParameters);

        using var timing = MethodTimingScope.Start("ExternalHttp", $"{method} SendAsync", request.EndpointName);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var httpRequest = CreateRequest(method, uri, request);
                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!endpoint.RetryEnabled || !ShouldRetry(response.StatusCode) || attempt >= _options.RetryCount)
                {
                    return new HttpApiResponse(response.IsSuccessStatusCode, (int)response.StatusCode, content);
                }
                await DelayBeforeRetryAsync(attempt + 1, request.EndpointName, cancellationToken);
            }
            catch (HttpRequestException) when (endpoint.RetryEnabled && attempt < _options.RetryCount)
            {
                await DelayBeforeRetryAsync(attempt + 1, request.EndpointName, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && endpoint.RetryEnabled && attempt < _options.RetryCount)
            {
                await DelayBeforeRetryAsync(attempt + 1, request.EndpointName, cancellationToken);
            }
        }
    }

    private HttpApiEndpointOptions ResolveEndpoint(string endpointName)
    {
        foreach (var endpoint in _options.Endpoints)
        {
            if (string.Equals(endpoint.Key, endpointName, StringComparison.OrdinalIgnoreCase))
            {
                return endpoint.Value;
            }
        }
        throw new KeyNotFoundException($"HTTP API endpoint '{endpointName}' is not configured.");
    }

    private static ApiHttpMethod ParseMethod(string method)
    {
        if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            return ApiHttpMethod.Get;
        if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            return ApiHttpMethod.Post;

        throw new ArgumentException($"Unsupported HTTP method: {method}", nameof(method));
    }

    private static HttpRequestMessage CreateRequest(ApiHttpMethod method, Uri uri, HttpApiRequest source)
    {
        var request = new HttpRequestMessage(method == ApiHttpMethod.Get ? HttpMethod.Get : HttpMethod.Post, uri);
        if (method == ApiHttpMethod.Post)
            request.Content = new StringContent(source.JsonBody ?? "{}", Encoding.UTF8, "application/json");

        foreach (var header in source.Headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return request;
    }

    private static Uri BuildUri(string endpoint, IReadOnlyDictionary<string, string?> queryParameters)
    {
        var builder = new UriBuilder(endpoint);
        if (queryParameters.Count == 0)
            return builder.Uri;

        var existingQuery = builder.Query.TrimStart('?');
        var query = string.Join("&", queryParameters
            .Where(pair => pair.Value is not null)
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));
        builder.Query = string.IsNullOrEmpty(existingQuery) ? query : $"{existingQuery}&{query}";
        return builder.Uri;
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.RequestTimeout)
            return true;
        if (statusCode == HttpStatusCode.TooManyRequests)
            return true;
        if (statusCode == HttpStatusCode.InternalServerError)
            return true;
        if (statusCode == HttpStatusCode.BadGateway)
            return true;
        if (statusCode == HttpStatusCode.ServiceUnavailable)
            return true;
        if (statusCode == HttpStatusCode.GatewayTimeout)
            return true;

        return false;
    }

    private async Task DelayBeforeRetryAsync(int attempt, string endpointName, CancellationToken cancellationToken)
    {
        var exponentialDelayMilliseconds = 200 * Math.Pow(2, attempt - 1);
        var jitterMilliseconds = Random.Shared.Next(0, 251);
        var delay = TimeSpan.FromMilliseconds(exponentialDelayMilliseconds + jitterMilliseconds);

        _logger.LogWarning(
            "[HTTP_RETRY] Endpoint={Endpoint} RetryAttempt={RetryAttempt} DelayMs={DelayMs:F0} JitterMs={JitterMs}",
            endpointName,
            attempt,
            delay.TotalMilliseconds,
            jitterMilliseconds);

        await Task.Delay(delay, cancellationToken);
    }
}
