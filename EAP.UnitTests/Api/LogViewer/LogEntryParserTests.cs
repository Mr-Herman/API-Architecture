using EAP.Api.LogViewer;

namespace EAP.UnitTests.Api.LogViewer;

public sealed class LogEntryParserTests
{
    private readonly LogEntryParser _parser = new();

    [Fact]
    public void Parse_NormalRequest_ReadsTimingChainAndRequestFields()
    {
        const string log = "2026-10-17 09:43:34.473 | INFO | [REQUEST] POST /fis/Equipment/SetEqpStatus | Status=200 | Total=81.7ms | Controller[EquipmentController].ChangeStatus=71.9ms > Application[EquipmentCommandService].ChangeStatusAsync=70.3ms > Database[DgFisOracle].ChangeStatusAsync=67.4ms | Trace=trace-01";

        var entry = _parser.Parse(log);

        Assert.NotNull(entry);
        Assert.Equal("INFO", entry.Level);
        Assert.Equal("REQUEST", entry.LogType);
        Assert.Equal("POST", entry.HttpMethod);
        Assert.Equal("/fis/Equipment/SetEqpStatus", entry.Path);
        Assert.Equal(200, entry.StatusCode);
        Assert.Equal(81.7, entry.TotalMilliseconds);
        Assert.Equal("trace-01", entry.TraceId);
        Assert.Equal(3, entry.TimingSteps.Count);
        Assert.Equal("Database", entry.TimingSteps[2].Layer);
        Assert.Equal(67.4, entry.TimingSteps[2].ElapsedMilliseconds);
    }

    [Fact]
    public void Parse_ErrorRequest_PreservesOriginalMultilineLog()
    {
        const string log = "2026-10-17 10:00:00.000 | ERROR | [UNHANDLED_ERROR] POST /fis/Production/TrackOut | Status=500 | Total=120.5ms | Database[DgFisOracle].ExecuteTrackOutAsync=80.0ms | FailedAt=Database[ProductionTrackingRepository].ExecuteTrackOutAsync | Code=5002 | Error=Oracle failed | Trace=trace-02 | Exception=System.InvalidOperationException: Oracle failed\r\n   at EAP.DataAccess.Repository.Execute()";

        var entry = _parser.Parse(log);

        Assert.NotNull(entry);
        Assert.Equal("UNHANDLED_ERROR", entry.LogType);
        Assert.Equal(5002, entry.ErrorCode);
        Assert.Contains("System.InvalidOperationException", entry.RawText);
        Assert.Contains("EAP.DataAccess.Repository.Execute", entry.RawText);
        Assert.Equal(log, entry.RawText);
    }
}
