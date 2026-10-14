using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Application.Services.Commands;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Moq;

namespace EAP.UnitTests.Tests;

public sealed class ThirdBatchCommandServiceTests
{
    [Fact]
    public async Task ConfirmOperator_WhenRepositoryChangesOneRecord_ReturnsSuccess()
    {
        var repository = new Mock<IEquipmentCommandRepository>();
        repository
            .Setup(x => x.InsertOperatorAsync(It.IsAny<OperatorBindingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new EquipmentCommandService(repository.Object, TestMessageLocalizer.Instance);
        var request = new OperatorBindingRequest
        {
            EquipmentId = "EQP-01",
            OperatorId = "EMP-01",
            WorkFlow = "FLOW-01",
            Process = "PROC-01"
        };

        var result = await service.ConfirmOperatorAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.AffectedCount);
        repository.Verify(
            x => x.InsertOperatorAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveOperator_WhenNothingChanges_ReturnsFailure()
    {
        var repository = new Mock<IEquipmentCommandRepository>();
        repository
            .Setup(x => x.RemoveOperatorAsync(It.IsAny<RemoveOperatorBindingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var service = new EquipmentCommandService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.RemoveOperatorAsync(
            new RemoveOperatorBindingRequest
            {
                EquipmentId = "EQP-01",
                OperatorId = "EMP-01"
            },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(4004, result.Code);
    }

    [Fact]
    public async Task ConfirmCarrierLotSetup_ResolvesEachLotBeforeWriting()
    {
        var commandRepository = new Mock<ICarrierCommandRepository>();
        commandRepository
            .Setup(x => x.ConfirmLotSetupAsync(It.IsAny<CarrierLotSetupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var queryRepository = new Mock<ICarrierQueryRepository>();
        queryRepository
            .Setup(x => x.ResolveLotIdAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync("CANONICAL-LOT-01");
        queryRepository
            .Setup(x => x.ResolveLotIdAsync("LOT-02", It.IsAny<CancellationToken>()))
            .ReturnsAsync("CANONICAL-LOT-02");

        var service = new CarrierCommandService(commandRepository.Object, queryRepository.Object, TestMessageLocalizer.Instance);
        var request = new CarrierLotSetupRequest
        {
            TransactionId = "TX-01",
            CarrierId = "CARRIER-01",
            Lots = ["LOT-01", "LOT-02"]
        };

        var result = await service.ConfirmLotSetupAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.AffectedCount);
        commandRepository.Verify(
            x => x.ConfirmLotSetupAsync(
                It.Is<CarrierLotSetupRequest>(value =>
                    value.Lots.Count == 2
                    && value.Lots[0] == "CANONICAL-LOT-01"
                    && value.Lots[1] == "CANONICAL-LOT-02"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmToolSetup_ReturnsAffectedCount()
    {
        var repository = new Mock<IToolCommandRepository>();
        repository
            .Setup(x => x.InsertSetupAsync(It.IsAny<ToolSetupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var service = new ToolCommandService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.ConfirmSetupAsync(
            new ToolSetupRequest
            {
                EquipmentId = "EQP-01",
                OperatorId = "EMP-01",
                Tools = [new ToolSetupRequest.Item { ToolId = "TOOL-01" }, new ToolSetupRequest.Item { ToolId = "TOOL-02" }]
            },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.AffectedCount);
    }

    [Fact]
    public async Task SendDashboardMessage_DelegatesToRepository()
    {
        var repository = new Mock<INotificationCommandRepository>();
        repository
            .Setup(x => x.SendDashboardMessageAsync(It.IsAny<DashboardMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new NotificationCommandService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.SendDashboardMessageAsync(
            new DashboardMessageRequest
            {
                EquipmentId = "EQP-01",
                Severity = "Warning",
                AlertType = "TEST",
                Message = "Test message",
                OperatorId = "EMP-01",
                WorkFlow = "FLOW-01",
                Process = "PROC-01"
            },
            CancellationToken.None);

        Assert.True(result.Success);
        repository.Verify(
            x => x.SendDashboardMessageAsync(It.IsAny<DashboardMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
