using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Application.Services;
using EAP.Models.Constants;
using EAP.Models.Requests.ProcessAssemblies;
using EAP.Models.Responses.ProcessAssemblies;
using EAP.Models.Entities.ProcessAssemblies;
using Moq;

namespace EAP.UnitTests.Tests;

public sealed class ProcessAssemblyServiceTests
{
    [Fact]
    public async Task SearchAsync_MapsFiltersPagingAndEntities()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        ProcessAssemblyQueryRequest? captured = null;
        repository.Setup(x => x.SearchAsync(It.IsAny<ProcessAssemblyQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ProcessAssemblyQueryRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(([new ProcessAssemblyEntity { Id = 8, JobNum = "JOB-8" }], 201));
        var service = CreateService(repository);

        var result = await service.SearchAsync(new ProcessAssemblyQueryRequest
        {
            JobNum = "  JOB-8  ",
            PageNumber = 2,
            PageSize = 50
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("JOB-8", captured?.JobNum);
        Assert.Equal(2, captured?.PageNumber);
        Assert.Equal(201, result.Data?.TotalCount);
        Assert.Equal(8, Assert.Single(result.Data!.Items).Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsResourceNotFound()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        repository.Setup(x => x.GetDetailsByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProcessAssemblyEntity?)null);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetByIdAsync(999, CancellationToken.None));

        Assert.Equal(BusinessCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsRelatedDefects()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        repository.Setup(x => x.GetDetailsByIdAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessAssemblyEntity
            {
                Id = 21,
                JobNum = "JOB-21",
                Defects = [new SnDefectDetailEntity
                {
                    ProjCode = "P1",
                    ProductCode = "PRODUCT",
                    ProcessName = "PROCESS",
                    LotNo = "LOT",
                    Sn = "SN001",
                    DefectCode = "D1",
                    Defect = "Test fail",
                    DefectQty = 1,
                    StartTime = DateTime.UtcNow.AddMinutes(-1),
                    TestTime = DateTime.UtcNow
                }]
            });
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(21, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("SN001", Assert.Single(result.Data!.Defects).Sn);
    }

    [Fact]
    public async Task CreateAsync_NormalizesInputAndReturnsGeneratedId()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        ProcessAssemblyEntity? saved = null;
        repository.Setup(x => x.CreateAsync(It.IsAny<ProcessAssemblyEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ProcessAssemblyEntity, CancellationToken>((entity, _) => saved = entity)
            .ReturnsAsync(200001);
        var service = CreateService(repository);

        var result = await service.CreateAsync(CreateRequest("  JOB-NEW  "), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(200001, result.Data?.Id);
        Assert.Equal("JOB-NEW", saved?.JobNum);
        Assert.Null(saved?.Remark);
    }

    [Fact]
    public async Task CreateAsync_WhenStartTimeIsAfterTestTime_ThrowsValidationError()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        var service = CreateService(repository);
        var request = CreateRequest("JOB-1");
        request.StartTime = new DateTime(2026, 10, 12, 10, 0, 0);
        request.TestTime = new DateTime(2026, 10, 12, 9, 0, 0);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(request, CancellationToken.None));

        Assert.Equal(BusinessCode.ParamError, exception.Code);
        Assert.Equal("StartTimeAfterTestTime", exception.Message);
        repository.Verify(
            x => x.CreateAsync(It.IsAny<ProcessAssemblyEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenInputQuantityIsLessThanOutputQuantity_ThrowsValidationError()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        var service = CreateService(repository);
        var request = UpdateRequest();
        request.InputQty = 5;
        request.OutputQty = 10;

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.UpdateAsync(10, request, CancellationToken.None));

        Assert.Equal(BusinessCode.ParamError, exception.Code);
        Assert.Equal("InputQuantityLessThanOutputQuantity", exception.Message);
        repository.Verify(
            x => x.UpdateAsync(It.IsAny<ProcessAssemblyEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenTimeRangeIsInvalid_ThrowsValidationError()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        var service = CreateService(repository);
        var request = new ProcessAssemblyQueryRequest
        {
            StartTimeFrom = new DateTime(2026, 10, 12, 10, 0, 0),
            StartTimeTo = new DateTime(2026, 10, 12, 9, 0, 0)
        };

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.SearchAsync(request, CancellationToken.None));

        Assert.Equal(BusinessCode.ParamError, exception.Code);
        Assert.Equal("StartTimeRangeInvalid", exception.Message);
        repository.Verify(
            x => x.SearchAsync(It.IsAny<ProcessAssemblyQueryRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowWasDeleted_ThrowsResourceNotFound()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        repository.Setup(x => x.UpdateAsync(It.IsAny<ProcessAssemblyEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.UpdateAsync(10, UpdateRequest(), CancellationToken.None));

        Assert.Equal(BusinessCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeleted_ReturnsSuccess()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        repository.Setup(x => x.DeleteAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(repository);

        var result = await service.DeleteAsync(10, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(BusinessCode.Success, result.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsResourceNotFound()
    {
        var repository = new Mock<IProcessAssemblyRepository>();
        repository.Setup(x => x.DeleteAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.DeleteAsync(10, CancellationToken.None));

        Assert.Equal(BusinessCode.NotFound, exception.Code);
    }

    private static ProcessAssemblyService CreateService(Mock<IProcessAssemblyRepository> repository)
    {
        var localizer = new Mock<IMessageLocalizer>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => key);
        return new ProcessAssemblyService(repository.Object, localizer.Object);
    }

    private static CreateProcessAssemblyRequest CreateRequest(string jobNum)
    {
        return new CreateProcessAssemblyRequest
        {
            JobNum = jobNum,
            PartNo = "PART-1",
            ProcessId = "PROC-1",
            ProductionLotNo = "LOT-1",
            Remark = " "
        };
    }

    private static UpdateProcessAssemblyRequest UpdateRequest()
    {
        return new UpdateProcessAssemblyRequest
        {
            JobNum = "JOB-1",
            PartNo = "PART-1",
            ProcessId = "PROC-1",
            ProductionLotNo = "LOT-1"
        };
    }
}
