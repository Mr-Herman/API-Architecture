using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Models.Common;
using EAP.Models.Requests.ProcessAssemblies;
using EAP.Models.Responses.ProcessAssemblies;
using Moq;

namespace EAP.UnitTests.Tests;

public sealed class ProcessAssembliesControllerTests
{
    [Fact]
    public async Task GetById_ForwardsIdAndReturnsServiceResult()
    {
        var service = new Mock<IProcessAssemblyService>();
        var expected = ApiResult<ProcessAssemblyResponse>.SuccessResult(CreateResponse(21));
        service.Setup(x => x.GetByIdAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ProcessAssembliesController(service.Object);

        var actual = await controller.GetById(21, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.GetByIdAsync(21, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_ForwardsRequestAndReturnsServiceResult()
    {
        var service = new Mock<IProcessAssemblyService>();
        var request = new ProcessAssemblyQueryRequest { JobNum = "JOB-1" };
        var expected = ApiResult<PagedResult<ProcessAssemblyResponse>>.SuccessResult(null);
        service.Setup(x => x.SearchAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new ProcessAssembliesController(service.Object);

        var actual = await controller.Search(request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.SearchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ForwardsIdAndRequest()
    {
        var service = new Mock<IProcessAssemblyService>();
        var request = new UpdateProcessAssemblyRequest { JobNum = "JOB-1", PartNo = "PART-1", ProcessId = "PROC-1", ProductionLotNo = "LOT-1" };
        var expected = ApiResult<ProcessAssemblyResponse>.SuccessResult(CreateResponse(21));
        service.Setup(x => x.UpdateAsync(21, request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new ProcessAssembliesController(service.Object);

        var actual = await controller.Update(21, request, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.UpdateAsync(21, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ForwardsRequestAndReturnsServiceResult()
    {
        var service = new Mock<IProcessAssemblyService>();
        var request = CreateRequest();
        var expected = ApiResult<ProcessAssemblyResponse>.SuccessResult(CreateResponse(200001));
        service.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ProcessAssembliesController(service.Object);

        var actual = await controller.Create(request, CancellationToken.None);

        Assert.Same(expected, actual);
        Assert.Equal(200001L, actual.Data?.Id);
        service.Verify(x => x.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ForwardsIdAndReturnsServiceResult()
    {
        var service = new Mock<IProcessAssemblyService>();
        var expected = ApiResult<object>.SuccessResult(null);
        service.Setup(x => x.DeleteAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ProcessAssembliesController(service.Object);

        var actual = await controller.Delete(21, CancellationToken.None);

        Assert.Same(expected, actual);
        service.Verify(x => x.DeleteAsync(21, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateProcessAssemblyRequest CreateRequest()
    {
        return new CreateProcessAssemblyRequest
        {
            JobNum = "JOB-1",
            PartNo = "PART-1",
            ProcessId = "PROC-1",
            ProductionLotNo = "LOT-1"
        };
    }

    private static ProcessAssemblyResponse CreateResponse(long id)
    {
        return new ProcessAssemblyResponse
        {
            Id = id,
            JobNum = "JOB-1",
            PartNo = "PART-1",
            ProcessId = "PROC-1",
            ProductionLotNo = "LOT-1"
        };
    }
}
