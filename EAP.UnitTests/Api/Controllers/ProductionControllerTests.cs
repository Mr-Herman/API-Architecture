using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Production;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Production;
using EAP.Models.Responses.Tools;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class ProductionControllerTests
{
    [Fact]
    public async Task GetProcessDefects_ForwardsRequest()
    {
        var service = new Mock<IProductionQueryService>();
        var request = new ProcessDefectRequest { WorkFlow = "FLOW", Process = "PROC" };
        var expected = ApiResult<IReadOnlyList<ProcessDefectResponse>>.SuccessResult(Array.Empty<ProcessDefectResponse>());
        service.Setup(x => x.GetProcessDefectsAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(service.Object);

        var actual = await controller.GetProcessDefects(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetProcessDefectsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLotInformation_ForwardsRequest()
    {
        var service = new Mock<IProductionQueryService>();
        var request = new LotInformationRequest { LotIdOrSn = "LOT-01", EquipmentId = "EQP-01" };
        var expected = ApiResult<LotInformationResponse>.SuccessResult(null);
        service.Setup(x => x.GetLotInformationAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(service.Object);

        var actual = await controller.GetLotInformation(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetLotInformationAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyTool_ForwardsRequest()
    {
        var service = new Mock<IToolCommandService>();
        var request = new VerifyToolRequest { EquipmentId = "EQP-01", ToolId = "TOOL-01", ToolUsage = 1 };
        var expected = ApiResult<ToolVerificationResponse>.SuccessResult(null);
        service.Setup(x => x.VerifyAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(toolCommandService: service.Object);

        var actual = await controller.VerifyTool(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.VerifyAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackIn_ForwardsRequest()
    {
        var service = new Mock<IProductionTrackingService>();
        var request = new TrackInRequest
        {
            TransactionId = "TX-IN-01",
            TransactionTime = DateTime.UtcNow,
            TraceWay = 1,
            LotId = "LOT-01",
            EquipmentId = "EQP-01",
            RecipeName = "RECIPE-01"
        };
        var expected = ApiResult<OperationResponse>.SuccessResult(new OperationResponse());
        service.Setup(x => x.TrackInAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(trackingService: service.Object);

        var actual = await controller.TrackIn(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.TrackInAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackOut_ForwardsRequest()
    {
        var service = new Mock<IProductionTrackingService>();
        var request = new TrackOutRequest
        {
            TransactionId = "TX-OUT-01",
            TransactionTime = DateTime.UtcNow,
            TraceWay = 1,
            LotId = "LOT-01",
            EquipmentId = "EQP-01"
        };
        var expected = ApiResult<OperationResponse>.SuccessResult(new OperationResponse());
        service.Setup(x => x.TrackOutAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(trackingService: service.Object);

        var actual = await controller.TrackOut(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.TrackOutAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ProductionController CreateController(
        IProductionQueryService? queryService = null,
        IToolCommandService? toolCommandService = null,
        IProductionTrackingService? trackingService = null)
    {
        return new ProductionController(
            queryService ?? new Mock<IProductionQueryService>().Object,
            toolCommandService ?? new Mock<IToolCommandService>().Object,
            trackingService ?? new Mock<IProductionTrackingService>().Object);
    }
}
