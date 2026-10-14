using System.Text.Json;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Responses.Operators;

namespace EAP.Application.Services;

internal static class OperatorAuthenticationResponseParser
{
    public static bool IsEquipmentAvailable(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            return root.TryGetProperty("status", out var status)
                && string.Equals(status.GetString(), "ok", StringComparison.OrdinalIgnoreCase)
                && root.TryGetProperty("data", out var data)
                && data.TryGetProperty("status_id", out var statusId)
                && statusId.ToString() == "5";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static OperatorProcessValidationResponse ParseQualification(
        string content,
        IMessageLocalizer localizer)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var status = root.TryGetProperty("status", out var statusElement)
                && statusElement.ValueKind == JsonValueKind.True;

            if (!status)
            {
                return OperatorProcessValidationResponseMapper.ToResponse(
                    false,
                    localizer["QualificationServiceFailed"]);
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return OperatorProcessValidationResponseMapper.ToResponse(
                    false,
                    localizer["QualificationNoData"]);
            }

            var flag = data.TryGetProperty("flag", out var flagElement)
                ? flagElement.GetString()
                : null;
            var info = data.TryGetProperty("info", out var infoElement)
                ? infoElement.GetString()
                : null;
            var authenticated = string.Equals(flag, "ok", StringComparison.OrdinalIgnoreCase);

            return OperatorProcessValidationResponseMapper.ToResponse(
                authenticated,
                authenticated ? info : info ?? localizer["OperatorNotQualified"]);
        }
        catch (JsonException)
        {
            return OperatorProcessValidationResponseMapper.ToResponse(
                false,
                localizer["QualificationResponseInvalid"]);
        }
    }
}
