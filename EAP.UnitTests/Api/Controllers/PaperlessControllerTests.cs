using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Models.Common;
using EAP.Models.Requests.Quality;
using EAP.Models.Responses.Quality;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class PaperlessControllerTests
{
    [Fact]
    public async Task GetSpcFaiResult_ForwardsRequest()
    {
        var service = new Mock<IQualityInspectionService>();
        var request = new QualityInspectionRequest
        {
            EquipmentId = "EQP-01",
            InspectType = "FAI",
            Project = "PROJECT-01",
            ProcessCode = "PROC-01"
        };
        var expected = ApiResult<QualityInspectionResponse>.SuccessResult(null);
        service
            .Setup(x => x.ValidateInspectionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new PaperlessController(service.Object);

        var actual = await controller.GetSpcFaiResult(
            request,
            CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(
            x => x.ValidateInspectionAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
