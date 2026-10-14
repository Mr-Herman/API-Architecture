using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Tools;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Tools;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class ToolsControllerTests
{
    [Fact]
    public async Task GetLotInformation_ForwardsRequest()
    {
        var service = new Mock<IToolQueryService>();
        var request = new ToolLotRequest { ToolId = "TOOL-01", EquipmentId = "EQP-01" };
        var expected = ApiResult<ToolLotInformationResponse>.SuccessResult(null);
        service.Setup(x => x.GetLotInformationAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new ToolsController(service.Object, new Mock<IToolCommandService>().Object);

        var actual = await controller.GetLotInformation(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetLotInformationAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEquipmentSetup_ForwardsRequest()
    {
        var service = new Mock<IToolQueryService>();
        var request = new EquipmentToolRequest { EquipmentId = "EQP-01" };
        var expected = ApiResult<IReadOnlyList<EquipmentToolResponse>>.SuccessResult(Array.Empty<EquipmentToolResponse>());
        service.Setup(x => x.GetEquipmentSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new ToolsController(service.Object, new Mock<IToolCommandService>().Object);

        var actual = await controller.GetEquipmentSetup(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetEquipmentSetupAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmSetup_ForwardsRequest()
    {
        var service = new Mock<IToolCommandService>();
        var request = new ToolSetupRequest { EquipmentId = "EQP-01", OperatorId = "EMP-01" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.ConfirmSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new ToolsController(new Mock<IToolQueryService>().Object, service.Object);

        var actual = await controller.ConfirmSetup(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ConfirmSetupAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
