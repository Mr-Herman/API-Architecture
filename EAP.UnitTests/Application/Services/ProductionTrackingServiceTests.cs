using EAP.Application.Interfaces;
using EAP.Application.Services;
using EAP.Models.Entities.Tracking;
using EAP.Models.Requests.Production;
using EAP.UnitTests.Tests;
using Moq;

namespace EAP.UnitTests.Application.Services;

public sealed class ProductionTrackingServiceTests
{
    [Fact]
    public async Task TrackIn_WhenTransactionWasCompleted_DoesNotWriteAgain()
    {
        var repository = new Mock<IProductionTrackingRepository>();
        repository
            .Setup(x => x.IsTransactionCompletedAsync("TX-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(repository);

        var result = await service.TrackInAsync(CreateTrackInRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, result.Data?.AffectedCount);
        repository.Verify(
            x => x.ExecuteTrackInAsync(
                It.IsAny<TrackInRequest>(),
                It.IsAny<ProductionTrackingContextEntity>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TrackIn_WhenContextIsValid_WritesTrackingData()
    {
        var repository = new Mock<IProductionTrackingRepository>();
        var request = CreateTrackInRequest();
        var context = CreateContext(1);
        repository
            .Setup(x => x.GetTrackInContextAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        repository
            .Setup(x => x.GetMaterialsAsync(
                It.IsAny<string>(),
                "FLOW",
                "PROC",
                "EQP-01",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository
            .Setup(x => x.ExecuteTrackInAsync(request, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var service = CreateService(repository);

        var result = await service.TrackInAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.AffectedCount);
        Assert.Equal("TX-01", result.Data?.TransactionId);
    }

    [Fact]
    public async Task TrackOut_WhenLotQuantityDoesNotMatch_ReturnsFailure()
    {
        var repository = CreateTrackOutRepository(traceWay: 1, trackInQuantity: 10);
        var request = CreateTrackOutRequest(1);
        request.TrackOutLotResultList =
        [
            new TrackOutRequest.LotResult { Quantity = 9, ProcessResult = "PASS" }
        ];
        var service = CreateService(repository);

        var result = await service.TrackOutAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("TrackOutQuantityMismatch", result.Message);
    }

    [Fact]
    public async Task TrackOut_WhenSerialNumberWasNotTrackedIn_ReturnsFailure()
    {
        var repository = CreateTrackOutRepository(traceWay: 2, trackInQuantity: 1);
        repository
            .Setup(x => x.GetSnWipsAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductionSnWipEntity
            {
                SerialNumber = "SN-01",
                Status = 0,
                Tester = "EQP-01"
            }]);
        var request = CreateTrackOutRequest(2);
        request.TrackOutSnResultList =
        [
            new TrackOutRequest.SnResult { SerialNumber = "SN-01", ProcessResult = "PASS" }
        ];
        var service = CreateService(repository);

        var result = await service.TrackOutAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("SnNotTrackedIn", result.Message);
    }

    [Fact]
    public async Task TrackOut_WhenSerialResultIsValid_WritesTrackingData()
    {
        var repository = CreateTrackOutRepository(traceWay: 2, trackInQuantity: 1);
        var request = CreateTrackOutRequest(2);
        var snWip = new ProductionSnWipEntity
        {
            SerialNumber = "SN-01",
            Status = 1,
            Tester = "EQP-01",
            WorkFlow = "FLOW",
            Process = "PROC"
        };
        repository
            .Setup(x => x.GetSnWipsAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([snWip]);
        request.TrackOutSnResultList =
        [
            new TrackOutRequest.SnResult { SerialNumber = "SN-01", ProcessResult = "PASS" }
        ];
        repository
            .Setup(x => x.ExecuteTrackOutAsync(
                request,
                It.IsAny<ProductionTrackingContextEntity>(),
                It.IsAny<ProductionLotWipEntity>(),
                It.IsAny<IReadOnlyList<ProductionSnWipEntity>>(),
                It.IsAny<IReadOnlyList<ProductionMaterialEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        var service = CreateService(repository);

        var result = await service.TrackOutAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3, result.Data?.AffectedCount);
    }

    private static ProductionTrackingService CreateService(
        Mock<IProductionTrackingRepository> repository)
    {
        return new ProductionTrackingService(repository.Object, TestMessageLocalizer.Instance);
    }

    private static Mock<IProductionTrackingRepository> CreateTrackOutRepository(
        int traceWay,
        int trackInQuantity)
    {
        var repository = new Mock<IProductionTrackingRepository>();
        repository
            .Setup(x => x.GetTrackOutContextAsync(
                It.IsAny<TrackOutRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateContext(traceWay));
        repository
            .Setup(x => x.GetLotWipAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionLotWipEntity
            {
                LotId = "LOT-01",
                WorkFlow = "FLOW",
                Process = "PROC",
                TraceWay = traceWay,
                TrackInQuantity = trackInQuantity,
                Status = 1
            });
        repository
            .Setup(x => x.GetDefectCodesAsync("FLOW", "PROC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        repository
            .Setup(x => x.GetMaterialsAsync(
                It.IsAny<string>(),
                "FLOW",
                "PROC",
                "EQP-01",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return repository;
    }

    private static ProductionTrackingContextEntity CreateContext(int traceWay)
    {
        return new ProductionTrackingContextEntity
        {
            EquipmentId = "EQP-01",
            OperatorId = "OP-01",
            WorkFlow = "FLOW",
            Process = "PROC",
            TraceWay = traceWay,
            LotId = "LOT-01",
            ProcessQuantity = 10
        };
    }

    private static TrackInRequest CreateTrackInRequest()
    {
        return new TrackInRequest
        {
            TransactionId = "TX-01",
            TransactionTime = new DateTime(2026, 10, 13, 8, 0, 0),
            TraceWay = 1,
            LotId = "LOT-01",
            EquipmentId = "EQP-01",
            RecipeName = "RECIPE-01"
        };
    }

    private static TrackOutRequest CreateTrackOutRequest(int traceWay)
    {
        return new TrackOutRequest
        {
            TransactionId = "TX-02",
            TransactionTime = new DateTime(2026, 10, 13, 9, 0, 0),
            TraceWay = traceWay,
            LotId = "LOT-01",
            EquipmentId = "EQP-01"
        };
    }
}
