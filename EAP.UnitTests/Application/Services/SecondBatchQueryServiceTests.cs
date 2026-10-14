using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Application.Http;
using EAP.Models.Entities.Operators;
using EAP.Models.Entities.Production;
using EAP.Application.Services;
using EAP.Models.Requests.Carriers;
using EAP.Models.Responses.Carriers;
using EAP.Models.Requests.Operators;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Operators;
using EAP.Models.Requests.Production;
using EAP.Models.Responses.Production;
using Moq;

namespace EAP.UnitTests.Tests;

public sealed class SecondBatchQueryServiceTests
{
    [Fact]
    public async Task OperatorAuthentication_WhenAllChecksPass_BindsOperator()
    {
        var repository = new Mock<IOperatorQueryRepository>();
        repository
            .Setup(x => x.GetOperatorAccountAsync("EMP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperatorAccountEntity
            {
                OperatorId = "EMP-01",
                Password = "HASH",
                OperatorName = "Operator One",
                Roles = "OPERATOR"
            });
        repository
            .Setup(x => x.GetProcessDefinitionAsync("FLOW-01", "PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDefinitionEntity
            {
                ProcessCode = "P001",
                ProjectCode = "PROJECT-01"
            });

        var commandRepository = new Mock<IEquipmentCommandRepository>();
        commandRepository
            .Setup(x => x.UpsertOperatorAsync(It.IsAny<OperatorBindingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var httpClient = new Mock<IHttpApiClient>();
        httpClient
            .SetupSequence(x => x.SendAsync(It.IsAny<HttpApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpApiResponse(true, 200, "{\"status\":\"ok\",\"data\":{\"status_id\":\"5\"}}"))
            .ReturnsAsync(new HttpApiResponse(true, 200, "{\"status\":true,\"data\":{\"flag\":\"ok\",\"info\":\"qualified\"}}"));

        var passwordEncoder = new Mock<ILegacyPasswordEncoder>();
        passwordEncoder.Setup(x => x.Encode("password")).Returns("HASH");
        var service = new OperatorAuthenticationService(
            repository.Object,
            commandRepository.Object,
            httpClient.Object,
            passwordEncoder.Object,
            TestMessageLocalizer.Instance);

        var result = await service.AuthenticateAsync(
            new AuthenticateOperatorRequest
            {
                OperatorId = "EMP-01",
                Password = "password",
                EquipmentId = "EQP-01",
                WorkFlow = "FLOW-01",
                Process = "PROC-01"
            },
            CancellationToken.None);

        Assert.True(result.Data?.Authenticated);
        Assert.Equal("Operator One", result.Data?.OperatorName);
        commandRepository.Verify(
            x => x.UpsertOperatorAsync(
                It.Is<OperatorBindingRequest>(request =>
                    request.EquipmentId == "EQP-01"
                    && request.OperatorId == "EMP-01"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OperatorValidation_WhenTrainingReturnsOk_ReturnsAuthenticated()
    {
        var repository = new Mock<IOperatorQueryRepository>();
        repository
            .Setup(x => x.HasWorkInProgressAsync("EQP-01", "EMP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.GetProcessDefinitionAsync("FLOW-01", "PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDefinitionEntity
            {
                Product = "FLOW-01",
                ProcessName = "PROC-01",
                ProcessCode = "P001",
                ProjectCode = "PROJECT-01"
            });

        var httpClient = new Mock<IHttpApiClient>();
        httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpApiResponse(true, 200, "{\"status\":true,\"data\":{\"flag\":\"ok\",\"info\":\"qualified\"}}"));

        var service = new OperatorQueryService(
            repository.Object,
            httpClient.Object,
            TestMessageLocalizer.Instance);

        var result = await service.ValidateProcessAsync(
            new ValidateOperatorProcessRequest
            {
                EquipmentId = "EQP-01",
                OperatorId = "EMP-01",
                WorkFlow = "FLOW-01",
                Process = "PROC-01"
            },
            CancellationToken.None);

        Assert.True(result.Data?.Authenticated);
    }

    [Fact]
    public async Task CarrierValidation_WhenLotIsAlreadyBound_ReturnsFailure()
    {
        var repository = new Mock<ICarrierQueryRepository>();
        repository
            .Setup(x => x.ResolveLotIdAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync("LOT-01");
        repository
            .Setup(x => x.IsLotBoundAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CarrierValidationService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.ValidateLotAsync(
            new ValidateLotCarrierRequest
            {
                CarrierId = "CAR-01",
                LotId = "LOT-01"
            },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("already bound", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductionDefects_ReturnsConfiguredDefects()
    {
        var repository = new Mock<IProductionQueryRepository>();
        repository
            .Setup(x => x.GetProcessDefectsAsync("FLOW-01", "PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProcessDefectEntity
            {
                WorkFlow = "FLOW-01",
                Process = "PROC-01",
                DefectCode = "D001",
                DefectName = "Open",
                FieldName = "DEFECT",
                TableName = "PROCESS_TABLE"
            }]);

        var service = new ProductionQueryService(repository.Object, TestMessageLocalizer.Instance);

        var result = await service.GetProcessDefectsAsync(
            new ProcessDefectRequest
            {
                WorkFlow = "FLOW-01",
                Process = "PROC-01"
            },
            CancellationToken.None);

        var defects = Assert.IsAssignableFrom<IReadOnlyList<ProcessDefectResponse>>(result.Data);
        Assert.Single(defects);
        Assert.Equal("D001", defects[0].DefectCode);
    }
}
