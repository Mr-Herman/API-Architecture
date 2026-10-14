using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Materials;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Materials;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class MaterialsControllerTests
{
    [Fact]
    public async Task GetEquipmentSetup_ForwardsRequest()
    {
        var service = new Mock<IMaterialQueryService>();
        var request = new EquipmentMaterialRequest { EquipmentId = "EQP-01" };
        var expected = ApiResult<IReadOnlyList<EquipmentMaterialResponse>>.SuccessResult(Array.Empty<EquipmentMaterialResponse>());
        service.Setup(x => x.GetEquipmentSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new MaterialsController(service.Object, new Mock<IMaterialCommandService>().Object);

        var actual = await controller.GetEquipmentSetup(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetEquipmentSetupAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInformation_ForwardsRequest()
    {
        var service = new Mock<IMaterialQueryService>();
        var request = new MaterialInformationRequest { MaterialLot = "LOT-01" };
        var expected = ApiResult<IReadOnlyList<MaterialInformationResponse>>.SuccessResult(Array.Empty<MaterialInformationResponse>());
        service.Setup(x => x.GetInformationAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new MaterialsController(service.Object, new Mock<IMaterialCommandService>().Object);

        var actual = await controller.GetInformation(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetInformationAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmSetup_ForwardsRequest()
    {
        var service = new Mock<IMaterialCommandService>();
        var request = new MaterialSetupRequest { EquipmentId = "EQP-01", TransactionId = "TX-01" };
        var expected = ApiResult<OperationResponse>.SuccessResult(null);
        service.Setup(x => x.ConfirmSetupAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new MaterialsController(new Mock<IMaterialQueryService>().Object, service.Object);

        var actual = await controller.ConfirmSetup(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.ConfirmSetupAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
