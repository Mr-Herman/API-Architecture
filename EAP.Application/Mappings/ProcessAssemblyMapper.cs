using EAP.Models.Requests.ProcessAssemblies;
using EAP.Models.Responses.ProcessAssemblies;
using EAP.Models.Entities.ProcessAssemblies;

namespace EAP.Application.Mappings;

internal static class ProcessAssemblyMapper
{
    public static ProcessAssemblyEntity ToEntity(ProcessAssemblyWriteRequest request)
    {
        return new ProcessAssemblyEntity
        {
            JobNum = request.JobNum.Trim(),
            PartNo = request.PartNo.Trim(),
            ProjCode = Normalize(request.ProjCode),
            ProductCode = Normalize(request.ProductCode),
            ProcessId = request.ProcessId.Trim(),
            ProductionLotNo = request.ProductionLotNo.Trim(),
            Marking = Normalize(request.Marking),
            Tester = Normalize(request.Tester),
            OperatorId = Normalize(request.OperatorId),
            InputQty = request.InputQty,
            OutputQty = request.OutputQty,
            StartTime = request.StartTime,
            TestTime = request.TestTime,
            Remark = Normalize(request.Remark)
        };
    }

    public static ProcessAssemblyResponse ToResponse(ProcessAssemblyEntity entity)
    {
        var defects = entity.Defects.Select(ToDefectResponse).ToArray();
        return CreateResponse(entity, defects);
    }

    private static ProcessAssemblyResponse CreateResponse(ProcessAssemblyEntity entity, IReadOnlyList<SnDefectResponse> defects)
    {
        return new ProcessAssemblyResponse
        {
            Id = entity.Id,
            JobNum = entity.JobNum,
            PartNo = entity.PartNo,
            ProjCode = entity.ProjCode,
            ProductCode = entity.ProductCode,
            ProcessId = entity.ProcessId,
            ProductionLotNo = entity.ProductionLotNo,
            Marking = entity.Marking,
            Tester = entity.Tester,
            OperatorId = entity.OperatorId,
            InputQty = entity.InputQty,
            OutputQty = entity.OutputQty,
            StartTime = entity.StartTime,
            TestTime = entity.TestTime,
            Remark = entity.Remark,
            Defects = defects
        };
    }

    private static SnDefectResponse ToDefectResponse(SnDefectDetailEntity defect)
    {
        return new SnDefectResponse
        {
            ProjCode = defect.ProjCode,
            ProductCode = defect.ProductCode,
            ProcessName = defect.ProcessName,
            LotNo = defect.LotNo,
            Sn = defect.Sn,
            DefectCode = defect.DefectCode,
            Defect = defect.Defect,
            DefectQty = defect.DefectQty,
            StartTime = defect.StartTime,
            TestTime = defect.TestTime
        };
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
