using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Models.Entities.Carriers;
using EAP.Models.Entities.Equipment;
using EAP.Models.Entities.Materials;
using EAP.Models.Entities.Tools;
using EAP.Application.Services;
using EAP.Models.Requests.Carriers;
using EAP.Models.Responses.Carriers;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using EAP.Models.Requests.Materials;
using EAP.Models.Responses.Materials;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;
using Moq;

namespace EAP.UnitTests.Tests;

public sealed class FirstBatchQueryServiceTests
{
    [Fact]
    public async Task EquipmentStatus_ReturnsDatabaseResult()
    {
        var repository = new Mock<IEquipmentQueryRepository>();
        repository
            .Setup(x => x.GetStatusAsync("EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EquipmentStatusEntity
            {
                EquipmentId = "EQP-01",
                Status = "RUN",
                SubStatus = "NORMAL",
                SubStatusDesc = "Running"
            });

        var service = new EquipmentQueryService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.GetStatusAsync(
            new EquipmentQueryRequest { EquipmentId = " EQP-01 " },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("RUN", result.Data?.Status);
    }

    [Fact]
    public async Task EquipmentStatus_WhenMissing_ThrowsNotFound()
    {
        var repository = new Mock<IEquipmentQueryRepository>();
        repository
            .Setup(x => x.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentStatusEntity?)null);

        var service = new EquipmentQueryService(repository.Object, TestMessageLocalizer.Instance);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetStatusAsync(
            new EquipmentQueryRequest { EquipmentId = "EQP-01" },
            CancellationToken.None));
    }

    [Fact]
    public async Task CarrierSetup_ReturnsLots()
    {
        var repository = new Mock<ICarrierQueryRepository>();
        repository
            .Setup(x => x.GetSetupAsync("CAR-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CarrierSetupEntity
            {
                CarrierId = "CAR-01",
                CarrierPn = "PN-01",
                CarrierFamily = "FAMILY-01",
                Lots = ["LOT-01"]
            });

        var service = new CarrierQueryService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.GetSetupAsync(
            new GetCarrierSetupRequest { CarrierId = "CAR-01" },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data!.Lots);
    }

    [Fact]
    public async Task MaterialQueries_ReturnResults()
    {
        var repository = new Mock<IMaterialQueryRepository>();
        repository
            .Setup(x => x.GetEquipmentSetupAsync("EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EquipmentMaterialEntity
            {
                EquipmentId = "EQP-01",
                ComponentName = "COMP-01",
                MaterialLot = "LOT-01",
                MaterialPn = "PN-01",
                OriginalQuantity = 10,
                RemainQuantity = 8
            }]);
        repository
            .Setup(x => x.GetInformationAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MaterialInformationEntity
            {
                MaterialLot = "LOT-01",
                MaterialPn = "PN-01",
                Quantity = 10
            }]);

        var service = new MaterialQueryService(repository.Object, TestMessageLocalizer.Instance);

        var setup = await service.GetEquipmentSetupAsync(
            new EquipmentMaterialRequest { EquipmentId = "EQP-01" },
            CancellationToken.None);
        var information = await service.GetInformationAsync(
            new MaterialInformationRequest { MaterialLot = "LOT-01" },
            CancellationToken.None);

        Assert.Single(setup.Data!);
        Assert.Single(information.Data!);
    }

    [Fact]
    public async Task ToolLot_WhenMissing_ThrowsNotFound()
    {
        var repository = new Mock<IToolQueryRepository>();
        repository
            .Setup(x => x.GetLotInformationAsync("TOOL-01", "EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolLotInformationEntity?)null);

        var service = new ToolQueryService(repository.Object, TestMessageLocalizer.Instance);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetLotInformationAsync(
            new ToolLotRequest { ToolId = "TOOL-01", EquipmentId = "EQP-01" },
            CancellationToken.None));
    }
}
