using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Carriers;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Carriers;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class CarriersControllerTests
{
    [Fact]
    public async Task GetSetup_ForwardsRequest()
    {
        var service = new Mock<ICarrierQueryService>();
        var request = new GetCarrierSetupRequest { CarrierId = "CAR-01" };
        var expected = ApiResult<CarrierSetupResponse>.SuccessResult(null);
        service.Setup(x => x.GetSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new CarriersController(service.Object, new Mock<ICarrierCommandService>().Object, new Mock<ICarrierValidationService>().Object);

        var actual = await controller.GetSetup(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetSetupAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateLot_ForwardsRequest()
    {
        var service = new Mock<ICarrierValidationService>();
        var request = new ValidateLotCarrierRequest { CarrierId = "CAR-01", LotId = "LOT-01" };
        var expected = ApiResult<ValidateLotCarrierResponse>.SuccessResult(null);
        service.Setup(x => x.ValidateLotAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new CarriersController(new Mock<ICarrierQueryService>().Object, new Mock<ICarrierCommandService>().Object, service.Object);

        var actual = await controller.ValidateLot(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ValidateLotAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmLotSetup_ForwardsRequest()
    {
        var service = new Mock<ICarrierCommandService>();
        var request = new CarrierLotSetupRequest { CarrierId = "CAR-01", Lots = ["LOT-01"] };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.ConfirmLotSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new CarriersController(new Mock<ICarrierQueryService>().Object, service.Object, new Mock<ICarrierValidationService>().Object);

        var actual = await controller.ConfirmLotSetup(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ConfirmLotSetupAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveLotSetup_ForwardsRequest()
    {
        var service = new Mock<ICarrierCommandService>();
        var request = new RemoveCarrierLotSetupRequest { CarrierId = "CAR-01", LotId = "LOT-01" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.RemoveLotSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new CarriersController(new Mock<ICarrierQueryService>().Object, service.Object, new Mock<ICarrierValidationService>().Object);

        var actual = await controller.RemoveLotSetup(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.RemoveLotSetupAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
