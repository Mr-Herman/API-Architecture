using EAP.Application.Interfaces;
using EAP.Application.Services;
using EAP.Models.Entities.Quality;
using EAP.Models.Requests.Quality;
using EAP.UnitTests.Tests;
using Moq;

namespace EAP.UnitTests.Application.Services;

public sealed class QualityInspectionServiceTests
{
    [Fact]
    public async Task ValidateInspection_WhenAllFaiRecordsPass_ReturnsPassed()
    {
        var qaRepository = new Mock<IQaInspectionRepository>();
        qaRepository
            .Setup(x => x.GetProcessCodeAsync("PROJECT-01", "PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync("FIS-PROC-01");
        qaRepository
            .Setup(x => x.IsEquipmentConfiguredAsync("FIS-PROC-01", "EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        qaRepository
            .Setup(x => x.GetLatestInspectionsAsync("EQP-01", "FIS-PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new FaiInspectionEntity { InspectionResult = "Pass" },
                new FaiInspectionEntity { InspectionResult = "Pass" }
            ]);
        var service = CreateService(qaRepository: qaRepository);

        var result = await service.ValidateInspectionAsync(
            CreateInspectionRequest("FAI"),
            CancellationToken.None);

        Assert.True(result.Data?.Passed);
        Assert.Equal("FAI", result.Data?.InspectionType);
    }

    [Fact]
    public async Task ValidateInspection_WhenSpcFails_ReturnsProblemDetail()
    {
        var spcRepository = new Mock<ISpcInspectionRepository>();
        spcRepository
            .Setup(x => x.GetProcessCodeAsync("PROJECT-01", "PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync("FIS-PROC-01");
        spcRepository
            .Setup(x => x.IsEquipmentConfiguredAsync("FIS-PROC-01", "EQP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        spcRepository
            .Setup(x => x.GetInspectionAsync("EQP-01", "FIS-PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpcInspectionEntity
            {
                Result = "FAIL",
                ProblemMessage = "CHART-01: value=12 > USL=10"
            });
        var service = CreateService(spcRepository: spcRepository);

        var result = await service.ValidateInspectionAsync(
            CreateInspectionRequest("PI"),
            CancellationToken.None);

        Assert.False(result.Data?.Passed);
        Assert.Equal("CHART-01: value=12 > USL=10", result.Data?.Reason);
        Assert.Equal("QualitySpcValidationFailed", result.Message);
    }

    [Fact]
    public async Task ValidateInspection_WhenTypeIsUnsupported_ReturnsValidationFailure()
    {
        var service = CreateService();

        var result = await service.ValidateInspectionAsync(
            CreateInspectionRequest("OTHER"),
            CancellationToken.None);

        Assert.False(result.Data?.Passed);
        Assert.Equal("QualityInspectionTypeInvalid", result.Message);
    }

    private static QualityInspectionService CreateService(
        Mock<IQaInspectionRepository>? qaRepository = null,
        Mock<ISpcInspectionRepository>? spcRepository = null)
    {
        return new QualityInspectionService(
            (qaRepository ?? new Mock<IQaInspectionRepository>()).Object,
            (spcRepository ?? new Mock<ISpcInspectionRepository>()).Object,
            TestMessageLocalizer.Instance);
    }

    private static QualityInspectionRequest CreateInspectionRequest(string inspectionType)
    {
        return new QualityInspectionRequest
        {
            EquipmentId = "EQP-01",
            InspectType = inspectionType,
            Project = "PROJECT-01",
            ProcessCode = "PROC-01"
        };
    }
}
