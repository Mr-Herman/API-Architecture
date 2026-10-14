using EAP.Api.Controllers;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class HoldLotsControllerTests
{
    [Fact]
    public async Task Hold_ForwardsRequest()
    {
        var service = new Mock<IHoldLotService>();
        var request = new HoldLotRequest { EquipmentId = "EQP-01", HoldReason = "TEST", OperatorId = "EMP-01" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.HoldAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new HoldLotsController(service.Object);

        var actual = await controller.Hold(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.HoldAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
