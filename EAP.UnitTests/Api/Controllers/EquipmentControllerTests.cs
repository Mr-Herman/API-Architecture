using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class EquipmentControllerTests
{
    [Fact]
    public async Task GetStatus_ForwardsRequest()
    {
        var service = new Mock<IEquipmentQueryService>();
        var request = new EquipmentQueryRequest { EquipmentId = "EQP-01" };
        var expected = ApiResult<EquipmentStatusResponse>.SuccessResult(null);
        service.Setup(x => x.GetStatusAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new EquipmentController(service.Object, new Mock<IEquipmentCommandService>().Object);

        var actual = await controller.GetStatus(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetStatusAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProcesses_ForwardsRequest()
    {
        var service = new Mock<IEquipmentQueryService>();
        var request = new EquipmentQueryRequest { EquipmentId = "EQP-01" };
        var expected = ApiResult<IReadOnlyList<EquipmentProcessResponse>>.SuccessResult(Array.Empty<EquipmentProcessResponse>());
        service.Setup(x => x.GetProcessesAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new EquipmentController(service.Object, new Mock<IEquipmentCommandService>().Object);

        var actual = await controller.GetProcesses(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetProcessesAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_ForwardsRequest()
    {
        var service = new Mock<IEquipmentCommandService>();
        var request = new EquipmentStatusChangeRequest { EquipmentId = "EQP-01", Status = "RUN" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.ChangeStatusAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new EquipmentController(new Mock<IEquipmentQueryService>().Object, service.Object);

        var actual = await controller.ChangeStatus(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ChangeStatusAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
