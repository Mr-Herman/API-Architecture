using EAP.Api.Extensions;
using EAP.Api.LogViewer;
using EAP.Application.Localization;
using EAP.Models.Common;
using EAP.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EAP.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("ops/logs")]
public sealed class LogViewerController : ControllerBase
{
    private readonly ILogFileReader _logFileReader;
    private readonly IMessageLocalizer _localizer;
    private readonly LogViewerOptions _options;
    private readonly IWebHostEnvironment _environment;

    public LogViewerController(ILogFileReader logFileReader, IMessageLocalizer localizer, IOptions<LogViewerOptions> options, IWebHostEnvironment environment)
    {
        _logFileReader = logFileReader;
        _localizer = localizer;
        _options = options.Value;
        _environment = environment;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (!_options.Enabled)
        {
            return NotFound();
        }

        var pagePath = Path.Combine(_environment.WebRootPath, "log-viewer", "index.html");

        return PhysicalFile(pagePath, "text/html; charset=utf-8");
    }

    [HttpGet("api")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<LogQueryPage>>> Query([FromQuery] LogQueryRequest request, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return NotFound();
        }

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(ApiResult<LogQueryPage>.FailResult(validationError, BusinessCode.ParamError));
        }

        var page = await _logFileReader.TryQueryAsync(request, cancellationToken);
        if (page is null)
        {
            Response.Headers.RetryAfter = "2";
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiResult<LogQueryPage>.FailResult(_localizer["LogViewerBusy"], BusinessCode.NotFound));
        }

        return ApiResult<LogQueryPage>.SuccessResult(page);
    }

    [HttpGet("settings")]
    [AllowAnonymous]
    public ActionResult<ApiResult<LogViewerSettingsResponse>> GetSettings()
    {
        if (!_options.Enabled)
        {
            return NotFound();
        }

        var settings = new LogViewerSettingsResponse
        {
            PageSize = LogViewerOptions.PageSize,
            MaxSearchDays = LogViewerOptions.MaxSearchDays
        };

        return ApiResult<LogViewerSettingsResponse>.SuccessResult(settings);
    }

    private string? ValidateRequest(LogQueryRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = request.StartDate ?? today;
        var endDate = request.EndDate ?? startDate;
        if (startDate > endDate)
        {
            return _localizer["LogViewerStartDateAfterEndDate"];
        }

        if (endDate > today)
        {
            return _localizer["LogViewerFutureDateNotAllowed"];
        }

        var searchDays = endDate.DayNumber - startDate.DayNumber + 1;
        if (searchDays > LogViewerOptions.MaxSearchDays)
        {
            return string.Format(_localizer["LogViewerSearchDaysExceeded"], LogViewerOptions.MaxSearchDays);
        }

        if (request.Page > LogViewerOptions.MaxPageNumber)
        {
            return string.Format(_localizer["LogViewerPageNumberExceeded"], LogViewerOptions.MaxPageNumber);
        }

        return null;
    }
}
