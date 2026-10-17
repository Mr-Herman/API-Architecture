using EAP.Api.Logging;
using Microsoft.AspNetCore.Http;

namespace EAP.UnitTests.Api.LogViewer;

public sealed class HttpPayloadSanitizerTests
{
    private readonly HttpPayloadSanitizer _sanitizer = new();

    [Fact]
    public void SanitizeJson_RedactsSensitiveFieldsAtEveryLevel()
    {
        const string body = """
            {
              "ClientId": "EQ-001",
              "ClientSecret": "secret-value",
              "Data": {
                "AccessToken": "token-value",
                "Password": "password-value"
              }
            }
            """;

        var result = _sanitizer.SanitizeJson(body, limitExceeded: false);

        Assert.NotNull(result);
        Assert.Contains("EQ-001", result);
        Assert.DoesNotContain("secret-value", result);
        Assert.DoesNotContain("token-value", result);
        Assert.DoesNotContain("password-value", result);
        Assert.Equal(3, result.Split("***REDACTED***").Length - 1);
    }

    [Fact]
    public void SanitizeJson_WhenSizeLimitExceeded_OmitsEntireBody()
    {
        var result = _sanitizer.SanitizeJson("{\"Value\":\"large\"}", limitExceeded: true);

        Assert.Equal("[BODY OMITTED: SIZE LIMIT EXCEEDED]", result);
    }

    [Fact]
    public void SanitizeJson_PreservesReadableChineseCharacters()
    {
        var result = _sanitizer.SanitizeJson("{\"Message\":\"成功\"}", limitExceeded: false);

        Assert.Equal("{\"Message\":\"成功\"}", result);
    }

    [Fact]
    public void SanitizeQuery_RedactsSensitiveParameters()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?equipmentId=EQ-001&token=secret-token");

        var result = _sanitizer.SanitizeQuery(context.Request.Query);

        Assert.NotNull(result);
        Assert.Contains("EQ-001", result);
        Assert.DoesNotContain("secret-token", result);
        Assert.Contains("***REDACTED***", result);
    }
}
