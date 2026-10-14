using EAP.Models.Entities.Production;
using EAP.Models.Responses.Production;

namespace EAP.Application.Mappings;

internal static class ProductionResponseMapper
{
    public static ProcessDefectResponse ToResponse(ProcessDefectEntity entity)
    {
        return new ProcessDefectResponse
        {
            WorkFlow = entity.WorkFlow,
            Process = entity.Process,
            DefectCode = entity.DefectCode,
            DefectName = entity.DefectName,
            FieldName = entity.FieldName,
            TableName = entity.TableName
        };
    }

    public static LotInformationResponse ToLotInformationResponse(
        ProductionEquipmentOperationEntity operation,
        ProductionLotEntity lot,
        ProductionRecipeEntity recipe,
        string requestedLotOrSerial,
        IReadOnlyList<string> serialNumbers,
        IReadOnlyList<string> toolRequirements,
        IReadOnlyList<ProductionMaterialRequirementEntity> materialRequirements)
    {
        return new LotInformationResponse
        {
            WorkFlow = operation.WorkFlow,
            TraceWay = operation.TraceWay,
            Process = operation.Process,
            ProcessCode = operation.ProcessCode,
            ProcessLeadTime = operation.ProcessLeadTime,
            BarcodeType = operation.TraceWay == 2
                && !string.Equals(lot.LotId, requestedLotOrSerial, StringComparison.OrdinalIgnoreCase)
                ? "SN"
                : "LOT",
            LotId = lot.LotId,
            ProcessQuantity = operation.TraceWay == 2
                ? serialNumbers.Count
                : lot.ProcessQuantity,
            ProjectCode = lot.ProjectCode,
            Recipe = recipe.Recipe,
            WorkOrder = lot.WorkOrder,
            ProductPartNumber = lot.ProductPartNumber,
            FormNo = lot.FormNo,
            LotMarking = lot.LotMarking,
            LotStatus = 0,
            SnStatus = operation.TraceWay == 2 ? 0 : null,
            OnlineRework = operation.OnlineRework,
            SerialNumbers = serialNumbers,
            ToolRequirements = toolRequirements,
            MaterialRequirements = materialRequirements
                .Select(ToMaterialRequirement)
                .ToArray()
        };
    }

    private static LotInformationResponse.Item ToMaterialRequirement(ProductionMaterialRequirementEntity entity)
    {
        return new LotInformationResponse.Item
        {
            ComponentName = entity.ComponentName,
            ComponentPartNumber = entity.ComponentPartNumber,
            ConsumptionFactor = entity.ConsumptionFactor
        };
    }
}
