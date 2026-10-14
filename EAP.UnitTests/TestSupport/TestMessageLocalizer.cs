using EAP.Application.Localization;

namespace EAP.UnitTests.Tests;

internal sealed class TestMessageLocalizer : IMessageLocalizer
{
    public static TestMessageLocalizer Instance { get; } = new();

    public string this[string key] => key switch
    {
        "LotAlreadyBound" => "This lot is already bound.",
        "LotNotFound" => "Lot ID does not exist.",
        "CarrierNotFound" => "Carrier information was not found.",
        "EquipmentStatusNotFound" => "Equipment status was not found.",
        "ToolLotInformationNotFound" => "Tool lot information was not found.",
        "NoOperatorProcessBinding" => "No operator process binding was found.",
        "Success" => "Success",
        "ProcessValidationFailed" => "Process validation failed.",
        "QualificationRequestFailed" => "Employee qualification service request failed.",
        "ToolInformationNotFound" => "Tool information was not found.",
        "ToolMustBeReset" => "The replaced tool must be reset before use.",
        "ToolLifeReached" => "The tool life has been reached.",
        "ToolVerificationPassed" => "Tool verification passed.",
        _ => key
    };
}
