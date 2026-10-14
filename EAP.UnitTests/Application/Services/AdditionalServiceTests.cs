using EAP.Application.Http;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Application.Services;
using EAP.Application.Services.Commands;
using EAP.Models.Entities.Carriers;
using EAP.Models.Entities.Equipment;
using EAP.Models.Entities.Operators;
using EAP.Models.Entities.Production;
using EAP.Models.Entities.Tools;
using EAP.Models.Requests.Carriers;
using EAP.Models.Requests.Equipment;
using EAP.Models.Requests.Operators;
using EAP.Models.Requests.Production;
using EAP.Models.Requests.Tools;
using EAP.Models.Requests.Transactions;
using EAP.UnitTests.Tests;
using Moq;

namespace EAP.UnitTests.Application.Services;

public sealed class AdditionalServiceTests
{
    [Fact]
    public async Task EquipmentQuery_GetProcesses_ReturnsMappedProcesses()
    {
        var repository = new Mock<IEquipmentQueryRepository>();
        repository.Setup(x => x.GetProcessesAsync("EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EquipmentProcessEntity { WorkFlow = "FLOW", Process = "PROC" }]);
        var service = new EquipmentQueryService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.GetProcessesAsync(
            new EquipmentQueryRequest { EquipmentId = " EQP-01 " }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        repository.Verify(x => x.GetProcessesAsync("EQP-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToolQuery_GetEquipmentSetup_ReturnsMappedTools()
    {
        var repository = new Mock<IToolQueryRepository>();
        repository.Setup(x => x.GetEquipmentSetupAsync("EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EquipmentToolEntity { ToolId = "TOOL-01", ToolPn = "PN-01" }]);
        var service = new ToolQueryService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.GetEquipmentSetupAsync(
            new EquipmentToolRequest { EquipmentId = " EQP-01 " }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("TOOL-01", Assert.Single(result.Data!).ToolId);
    }

    [Fact]
    public async Task OperatorQuery_GetProcessOperators_WhenFound_ReturnsOperators()
    {
        var repository = new Mock<IOperatorQueryRepository>();
        repository.Setup(x => x.GetProcessOperatorsAsync("EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperatorProcessEntity { WorkFlow = "FLOW", Process = "PROC", Operators = ["EMP-01"] });
        var service = new OperatorQueryService(
            repository.Object,
            new Mock<IHttpApiClient>().Object,
            TestMessageLocalizer.Instance);

        var result = await service.GetProcessOperatorsAsync(
            new OperatorEquipmentQueryRequest { EquipmentId = " EQP-01 " }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("EMP-01", Assert.Single(result.Data!.Operators));
    }

    [Fact]
    public async Task CarrierValidation_WhenLotAndCarrierAreValid_ReturnsCanonicalLot()
    {
        var repository = new Mock<ICarrierQueryRepository>();
        repository.Setup(x => x.ResolveLotIdAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync("LOT-01-CANONICAL");
        repository.Setup(x => x.IsLotBoundAsync("LOT-01-CANONICAL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.GetSetupAsync("CAR-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CarrierSetupEntity { CarrierId = "CAR-01", CarrierPn = "PN-01" });
        var service = new CarrierValidationService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.ValidateLotAsync(
            new ValidateLotCarrierRequest { CarrierId = " CAR-01 ", LotId = " LOT-01 " }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("LOT-01-CANONICAL", result.Data?.LotId);
    }

    [Fact]
    public async Task ProductionQuery_GetLotInformation_WhenTraceWayIsLot_ReturnsLotDetails()
    {
        var repository = new Mock<IProductionQueryRepository>();
        repository.Setup(x => x.GetEquipmentOperationAsync("EQP-01", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionEquipmentOperationEntity
            {
                WorkFlow = "FLOW",
                Process = "PROC",
                ProcessCode = "P001",
                TraceWay = 1,
                ProcessLeadTime = 10,
                OnlineRework = 0
            });
        repository.Setup(x => x.GetLotMasterAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionLotEntity
            {
                LotId = "LOT-01",
                ProjectCode = "PROJ-01",
                ProductPartNumber = "PART-01",
                FormNo = "FORM-01",
                ProcessQuantity = 10,
                IsFinished = "N"
            });
        repository.Setup(x => x.GetRecipeAsync("PROJ-01", "FLOW", "PROC", "FORM-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionRecipeEntity { Recipe = "RECIPE-01" });
        repository.Setup(x => x.GetToolRequirementsAsync("PART-01", "FLOW", "PROC", "EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(["TOOL-01"]);
        repository.Setup(x => x.GetMaterialRequirementsAsync("PART-01", "FLOW", "PROC", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductionMaterialRequirementEntity { ComponentName = "MAT-01" }]);
        var service = new ProductionQueryService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.GetLotInformationAsync(
            new LotInformationRequest { LotIdOrSn = " LOT-01 ", EquipmentId = " EQP-01 " }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("LOT-01", result.Data?.LotId);
        Assert.Equal("RECIPE-01", result.Data?.Recipe);
        Assert.Single(result.Data!.ToolRequirements);
    }

    [Fact]
    public async Task EquipmentCommand_ChangeStatus_WhenRepositorySucceeds_ReturnsSuccess()
    {
        var repository = new Mock<IEquipmentCommandRepository>();
        var request = new EquipmentStatusChangeRequest { EquipmentId = "EQP-01", Status = "RUN" };
        repository.Setup(x => x.ChangeStatusAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var service = new EquipmentCommandService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.ChangeStatusAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.AffectedCount);
    }

    [Fact]
    public async Task CarrierCommand_RemoveLotSetup_WhenNothingChanged_ReturnsFailure()
    {
        var repository = new Mock<ICarrierCommandRepository>();
        var request = new RemoveCarrierLotSetupRequest { CarrierId = "CAR-01", LotId = "LOT-01" };
        repository.Setup(x => x.RemoveLotSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var service = new CarrierCommandService(
            repository.Object,
            new Mock<ICarrierQueryRepository>().Object,
            TestMessageLocalizer.Instance);

        var result = await service.RemoveLotSetupAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(4004, result.Code);
    }

    [Fact]
    public async Task HoldLot_WhenRepositorySucceeds_ReturnsAffectedCount()
    {
        var repository = new Mock<IHoldLotRepository>();
        var request = new HoldLotRequest { EquipmentId = "EQP-01", HoldReason = "TEST", OperatorId = "EMP-01" };
        repository.Setup(x => x.GetEquipmentProcessAsync("EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("FLOW-01", "PROC-01", "EMP-02"));
        repository.Setup(x => x.GetNextHoldNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1001m);
        repository.Setup(x => x.HoldAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        var service = new HoldLotService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.HoldAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.AffectedCount);
        Assert.Equal("FLOW-01", request.WorkFlow);
        Assert.Equal("PROC-01", request.Process);
        Assert.Equal("EMP-02", request.OperatorId);
        Assert.Equal(1001m, request.HoldNumber);
    }

    [Fact]
    public async Task MaterialCommand_ConfirmSetup_ReturnsAffectedCount()
    {
        var repository = new Mock<IMaterialCommandRepository>();
        var request = new MaterialSetupRequest { EquipmentId = "EQP-01", TransactionId = "TX-01" };
        repository.Setup(x => x.ReplaceSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var service = new MaterialCommandService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.ConfirmSetupAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.AffectedCount);
    }

    [Fact]
    public async Task ToolCommand_Verify_WhenPassed_ReturnsSuccess()
    {
        var repository = new Mock<IToolCommandRepository>();
        var request = new VerifyToolRequest { EquipmentId = "EQP-01", ToolId = "TOOL-01", ToolUsage = 1 };
        repository.Setup(x => x.VerifyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EAP.Models.Responses.Tools.ToolVerificationResponse { Passed = true, Message = "ToolVerified" });
        var service = new ToolCommandService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.VerifyAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data?.Passed);
    }
}
