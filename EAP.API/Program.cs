using EAP.Api.Extensions;
using EAP.Api.Middlewares;
using EAP.Application;
using EAP.Infrastructure;
using EAP.DataAccess;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

var requireToken = builder.Configuration.GetValue<bool>("Security:RequireToken");
var authenticationRequired = !builder.Environment.IsDevelopment() || requireToken;

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddApiControllers();
builder.Services.AddApiLocalization();
builder.Services.AddApiDocumentation(authenticationRequired);
builder.Services.AddApiAuthentication(builder.Configuration, authenticationRequired);
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddApiHealthChecks(builder.Configuration);
builder.Services.AddApiConfigurationValidation(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddExternalHttpApiClient();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddInfrastructureDataServices();

var app = builder.Build();

app.UseMiddleware<TraceIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRequestLocalization();
app.UseMiddleware<SecurityHeaderMiddleware>();
app.UseMiddleware<JwtUnauthorizedMiddleware>();

app.UseApiDocumentation();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapApiHealthEndpoints();
app.MapControllers();

app.Run();

public partial class Program { }
