using System.Collections.Concurrent;
using System.Net;
using EAP.Application.Http;
using EAP.Infrastructure.Http;
using EAP.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EAP.UnitTests.Tests;

public sealed class HttpApiClientTests
{
    [Fact]
    public async Task SendAsync_UsesConfiguredPostByDefault()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}")
        });
        var client = CreateClient(handler);
        var response = await client.SendAsync(new HttpApiRequest("OaTrainingRecord")
        {
            JsonBody = "{\"employeeNo\":\"1001\"}"
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("http://oa.test/api/training", handler.LastUri?.ToString());
        Assert.Equal("{\"employeeNo\":\"1001\"}", handler.LastBody);
    }

    [Fact]
    public async Task SendAsync_CanOverrideEndpointToGetAndEncodeQuery()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);
        await client.SendAsync(new HttpApiRequest("OaEquipmentInfoBySn")
        {
            Method = ApiHttpMethod.Get,
            QueryParameters = new Dictionary<string, string?> { ["sn"] = "A B/01" }
        });

        Assert.Equal(HttpMethod.Get, handler.LastMethod);
        Assert.Equal("https://oa.test/api/equipment?sn=A%20B%2F01", handler.LastUri?.AbsoluteUri);
        Assert.Null(handler.LastBody);
    }

    [Fact]
    public async Task SendAsync_RetriesTransientServerFailure()
    {
        HttpResponseMessage CreateResponse(int attempt)
        {
            if (attempt == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        var handler = new RecordingHandler(CreateResponse);
        var client = CreateClient(handler, retryCount: 1);
        var request = new HttpApiRequest("OaTrainingRecord");
        var response = await client.SendAsync(request);

        Assert.True(response.IsSuccess);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task SendAsync_SupportsConcurrentRequests()
    {
        const int requestCount = 50;

        HttpResponseMessage CreateResponse(int callNumber)
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        var handler = new RecordingHandler(CreateResponse);
        var client = CreateClient(handler);
        var startSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = new List<Task<HttpApiResponse>>();

        for (var index = 0; index < requestCount; index++)
        {
            var requestNumber = index;
            tasks.Add(Task.Run(async () =>
            {
                await startSignal.Task;
                var request = new HttpApiRequest("OaTrainingRecord")
                {
                    JsonBody = $"{{\"requestNumber\":{requestNumber}}}"
                };
                return await client.SendAsync(request);
            }));
        }
        startSignal.SetResult(true);
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(requestCount, handler.CallCount);
        Assert.Equal(requestCount, handler.Requests.Count);
        Assert.All(responses, response =>
        {
            Assert.True(response.IsSuccess);
        });
    }

    private static HttpApiClient CreateClient(RecordingHandler handler, int retryCount = 0)
    {
        var trainingEndpoint = new HttpApiEndpointOptions
        {
            Url = "http://oa.test/api/training",
            DefaultMethod = "POST",
            RetryEnabled = true
        };

        var equipmentEndpoint = new HttpApiEndpointOptions
        {
            Url = "https://oa.test/api/equipment",
            DefaultMethod = "POST"
        };

        var endpoints = new Dictionary<string, HttpApiEndpointOptions>();
        endpoints.Add("OaTrainingRecord", trainingEndpoint);
        endpoints.Add("OaEquipmentInfoBySn", equipmentEndpoint);

        var httpApiOptions = new HttpApiOptions
        {
            TimeoutSeconds = 10,
            RetryCount = retryCount,
            Endpoints = endpoints
        };

        var options = Options.Create(httpApiOptions);
        var httpClient = new HttpClient(handler);
        var logger = NullLogger<HttpApiClient>.Instance;

        return new HttpApiClient(httpClient, options, logger);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _responseFactory;
        private readonly ConcurrentQueue<RecordedRequest> _requests;
        private int _callCount;

        public RecordingHandler(Func<int, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
            _requests = new ConcurrentQueue<RecordedRequest>();
        }

        public int CallCount
        {
            get
            {
                return Volatile.Read(ref _callCount);
            }
        }

        public IReadOnlyList<RecordedRequest> Requests
        {
            get
            {
                return _requests.ToArray();
            }
        }

        public HttpMethod? LastMethod
        {
            get
            {
                return _requests.LastOrDefault()?.Method;
            }
        }

        public Uri? LastUri
        {
            get
            {
                return _requests.LastOrDefault()?.Uri;
            }
        }

        public string? LastBody
        {
            get
            {
                return _requests.LastOrDefault()?.Body;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var callNumber = Interlocked.Increment(ref _callCount);
            string? body;

            if (request.Content is null)
            {
                body = null;
            }
            else
            {
                body =
                    await request.Content.ReadAsStringAsync(
                        cancellationToken);
            }

            var recordedRequest = new RecordedRequest(
                callNumber,
                request.Method,
                request.RequestUri,
                body);

            _requests.Enqueue(recordedRequest);

            return _responseFactory(callNumber);
        }
    }

    private sealed class RecordedRequest
    {
        public RecordedRequest(int callNumber, HttpMethod method, Uri? uri, string? body)
        {
            CallNumber = callNumber;
            Method = method;
            Uri = uri;
            Body = body;
        }

        public int CallNumber { get; }

        public HttpMethod Method { get; }

        public Uri? Uri { get; }

        public string? Body { get; }
    }
}
