using EAP.Api.Controllers;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class NotificationsControllerTests
{
    [Fact]
    public async Task SendDashboardMessage_ForwardsRequest()
    {
        var service = new Mock<INotificationCommandService>();
        var request = new DashboardMessageRequest { EquipmentId = "EQP-01", Severity = "Warning", AlertType = "TEST", Message = "Message", OperatorId = "EMP-01", WorkFlow = "FLOW", Process = "PROC" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.SendDashboardMessageAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new NotificationsController(service.Object);

        var actual = await controller.SendDashboardMessage(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.SendDashboardMessageAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
