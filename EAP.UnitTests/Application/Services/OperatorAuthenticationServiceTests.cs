using EAP.Application.Http;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Application.Services;
using EAP.Models.Entities.Operators;
using EAP.Models.Requests.Operators;
using EAP.Models.Requests.Transactions;
using EAP.UnitTests.Tests;
using Moq;

namespace EAP.UnitTests.Application.Services;

public sealed class OperatorAuthenticationServiceTests
{
    [Fact]
    public async Task ValidateCredentials_WhenPasswordMatches_ReturnsAuthenticatedOperator()
    {
        var repository = new Mock<IOperatorQueryRepository>();
        repository
            .Setup(x => x.GetOperatorAccountAsync("EMP-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperatorAccountEntity
            {
                OperatorId = "EMP-01",
                Password = "ENCODED",
                OperatorName = "Operator One",
                Roles = "OPERATOR"
            });
        var passwordEncoder = new Mock<ILegacyPasswordEncoder>();
        passwordEncoder.Setup(x => x.Encode("plain-password")).Returns("ENCODED");
        var service = CreateService(repository, passwordEncoder: passwordEncoder);

        var result = await service.ValidateCredentialsAsync(
            new ValidateOperatorCredentialsRequest
            {
                OperatorId = " EMP-01 ",
                Password = "plain-password"
            },
            CancellationToken.None);

        Assert.True(result.Data?.Authenticated);
        Assert.Equal("Operator One", result.Data?.OperatorName);
        passwordEncoder.Verify(x => x.Encode("plain-password"), Times.Once);
    }

    [Fact]
    public async Task ValidateEquipmentAvailability_WhenOaStatusIsFive_ReturnsAvailable()
    {
        HttpApiRequest? sentRequest = null;
        var httpClient = new Mock<IHttpApiClient>();
        httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<HttpApiRequest, CancellationToken>((request, _) => sentRequest = request)
            .ReturnsAsync(new HttpApiResponse(
                true,
                200,
                "{\"status\":\"ok\",\"data\":{\"status_id\":\"5\"}}"));
        var service = CreateService(httpClient: httpClient);

        var result = await service.ValidateEquipmentAvailabilityAsync(
            new ValidateEquipmentAvailabilityRequest { EquipmentId = " EQP-01 " },
            CancellationToken.None);

        Assert.True(result.Data?.Available);
        Assert.Equal(ApiHttpMethod.Get, sentRequest?.Method);
        Assert.Equal("EQP-01", sentRequest?.QueryParameters["SN"]);
    }

    [Fact]
    public async Task ValidateQualification_WhenOaReturnsOk_ReturnsAuthenticated()
    {
        var repository = new Mock<IOperatorQueryRepository>();
        repository
            .Setup(x => x.GetProcessDefinitionAsync("FLOW-01", "PROC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDefinitionEntity
            {
                ProcessCode = "P001",
                ProjectCode = "PROJECT-01"
            });
        HttpApiRequest? sentRequest = null;
        var httpClient = new Mock<IHttpApiClient>();
        httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<HttpApiRequest, CancellationToken>((request, _) => sentRequest = request)
            .ReturnsAsync(new HttpApiResponse(
                true,
                200,
                "{\"status\":true,\"data\":{\"flag\":\"ok\",\"info\":\"qualified\"}}"));
        var service = CreateService(repository, httpClient: httpClient);

        var result = await service.ValidateQualificationAsync(
            new ValidateOperatorQualificationRequest
            {
                OperatorId = "EMP-01",
                WorkFlow = "FLOW-01",
                Process = "PROC-01"
            },
            CancellationToken.None);

        Assert.True(result.Data?.Authenticated);
        Assert.Equal("P001", sentRequest?.QueryParameters["process_code"]);
        Assert.Equal("PROJECT-01", sentRequest?.QueryParameters["proj_code"]);
        Assert.Equal("EMP-01", sentRequest?.QueryParameters["emp_code"]);
    }

    [Fact]
    public async Task LoginOperator_WhenWriteSucceeds_ReturnsAffectedCount()
    {
        var commandRepository = new Mock<IEquipmentCommandRepository>();
        commandRepository
            .Setup(x => x.UpsertOperatorAsync(
                It.Is<OperatorBindingRequest>(request =>
                    request.OperatorId == "EMP-01"
                    && request.EquipmentId == "EQP-01"
                    && request.WorkFlow == "FLOW-01"
                    && request.Process == "PROC-01"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = CreateService(commandRepository: commandRepository);

        var result = await service.LoginOperatorAsync(
            new LoginOperatorRequest
            {
                OperatorId = " EMP-01 ",
                EquipmentId = " EQP-01 ",
                WorkFlow = " FLOW-01 ",
                Process = " PROC-01 "
            },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.AffectedCount);
    }

    private static OperatorAuthenticationService CreateService(
        Mock<IOperatorQueryRepository>? repository = null,
        Mock<IEquipmentCommandRepository>? commandRepository = null,
        Mock<IHttpApiClient>? httpClient = null,
        Mock<ILegacyPasswordEncoder>? passwordEncoder = null)
    {
        return new OperatorAuthenticationService(
            (repository ?? new Mock<IOperatorQueryRepository>()).Object,
            (commandRepository ?? new Mock<IEquipmentCommandRepository>()).Object,
            (httpClient ?? new Mock<IHttpApiClient>()).Object,
            (passwordEncoder ?? new Mock<ILegacyPasswordEncoder>()).Object,
            TestMessageLocalizer.Instance);
    }
}
