namespace EAP.Application.Http;

public sealed class HttpApiRequest
{
    public HttpApiRequest(string endpointName)
    {
        if (string.IsNullOrWhiteSpace(endpointName))
        {
            throw new ArgumentException("Endpoint name is required.", nameof(endpointName));
        }

        EndpointName = endpointName;
        QueryParameters = new Dictionary<string, string?>();
        Headers = new Dictionary<string, string>();
    }

    public string EndpointName { get; }

    public ApiHttpMethod? Method { get; set; }

    public string? JsonBody { get; set; }

    public IReadOnlyDictionary<string, string?> QueryParameters { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; }
}
