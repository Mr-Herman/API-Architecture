using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EAP.Api.Logging;

public sealed class HttpPayloadSanitizer
{
    private const string RedactedValue = "***REDACTED***";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AccessToken",
        "Authorization",
        "ClientSecret",
        "ConnectionString",
        "Password",
        "RefreshToken",
        "SigningKey",
        "Token"
    };

    public string? SanitizeJson(string? body, bool limitExceeded)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        if (limitExceeded)
        {
            return "[BODY OMITTED: SIZE LIMIT EXCEEDED]";
        }

        try
        {
            var node = JsonNode.Parse(body);
            Redact(node);
            return node?.ToJsonString(JsonOptions);
        }
        catch (JsonException)
        {
            return "[BODY OMITTED: INVALID JSON]";
        }
    }

    public string? SanitizeQuery(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            return null;
        }

        var result = new JsonObject();
        foreach (var parameter in query)
        {
            if (SensitiveNames.Contains(parameter.Key))
            {
                result[parameter.Key] = RedactedValue;
                continue;
            }

            result[parameter.Key] = parameter.Value.Count == 1
                ? parameter.Value[0]
                : JsonSerializer.SerializeToNode(parameter.Value.ToArray());
        }

        return result.ToJsonString(JsonOptions);
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToArray())
            {
                if (SensitiveNames.Contains(property.Key))
                {
                    jsonObject[property.Key] = RedactedValue;
                }
                else
                {
                    Redact(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                Redact(item);
            }
        }
    }
}
