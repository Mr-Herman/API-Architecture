using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Requests.Operators;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Equipment;
using EAP.Models.Responses.Operators;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class OperatorsControllerTests
{
    [Fact]
    public async Task GetEquipmentProcess_ForwardsRequest()
    {
        var service = new Mock<IOperatorQueryService>();
        var request = new OperatorEquipmentQueryRequest { EquipmentId = "EQP-01" };
        var expected = ApiResult<OperatorProcessResponse>.SuccessResult(null);
        service.Setup(x => x.GetProcessOperatorsAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new OperatorsController(
            service.Object,
            new Mock<IOperatorAuthenticationService>().Object,
            new Mock<IEquipmentCommandService>().Object);

        var actual = await controller.GetEquipmentProcess(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetProcessOperatorsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateProcess_ForwardsRequest()
    {
        var service = new Mock<IOperatorQueryService>();
        var request = new ValidateOperatorProcessRequest { EquipmentId = "EQP-01", OperatorId = "EMP-01", WorkFlow = "FLOW", Process = "PROC" };
        var expected = ApiResult<OperatorProcessValidationResponse>.SuccessResult(null);
        service.Setup(x => x.ValidateProcessAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new OperatorsController(
            service.Object,
            new Mock<IOperatorAuthenticationService>().Object,
            new Mock<IEquipmentCommandService>().Object);

        var actual = await controller.ValidateProcess(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ValidateProcessAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmBinding_ForwardsRequest()
    {
        var service = new Mock<IEquipmentCommandService>();
        var request = new OperatorBindingRequest { EquipmentId = "EQP-01", OperatorId = "EMP-01", WorkFlow = "FLOW", Process = "PROC" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.ConfirmOperatorAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new OperatorsController(
            new Mock<IOperatorQueryService>().Object,
            new Mock<IOperatorAuthenticationService>().Object,
            service.Object);

        var actual = await controller.ConfirmBinding(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ConfirmOperatorAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveBinding_ForwardsRequest()
    {
        var service = new Mock<IEquipmentCommandService>();
        var request = new RemoveOperatorBindingRequest { EquipmentId = "EQP-01", OperatorId = "EMP-01" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.RemoveOperatorAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new OperatorsController(
            new Mock<IOperatorQueryService>().Object,
            new Mock<IOperatorAuthenticationService>().Object,
            service.Object);

        var actual = await controller.RemoveBinding(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.RemoveOperatorAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SimulateOa_ForwardsRequestToStatusChange()
    {
        var service = new Mock<IEquipmentCommandService>();
        var request = new EquipmentStatusChangeRequest { EquipmentId = "EQP-01", Status = "RUN" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.ChangeStatusAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new OperatorsController(
            new Mock<IOperatorQueryService>().Object,
            new Mock<IOperatorAuthenticationService>().Object,
            service.Object);

        var actual = await controller.SimulateOa(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ChangeStatusAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
