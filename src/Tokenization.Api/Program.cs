using Tokenization.Api.Authorization.Config;
using Tokenization.Api.Config;
using Tokenization.Api.Config.Options;
using Tokenization.Api.Controllers.Config;
using Tokenization.Api.ExceptionHandling.Config;
using Tokenization.Api.Health.Config;
using Tokenization.Api.Idempotency.Config;
using Tokenization.Api.Logging.Config;
using Tokenization.Api.OpenApi.Config;
using Tokenization.Api.RateLimiting.Config;
using Tokenization.Api.Security.Config;
using Tokenization.Api.Versioning.Config;
using Tokenization.Application.Config;
using Tokenization.Infrastructure.Config;
using Tokenization.Infrastructure.Db.Config;

var builder = WebApplication.CreateBuilder(args);

builder.AddTokenizationLogging();
builder.Services.AddTokenizationControllers();
builder.Services.AddTokenizationVersioning();
builder.Services.AddProblemDetails();
builder.Services.AddTokenizationAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddTokenizationAuthorization();
builder.Services.AddTokenizationSecurity(builder.Configuration);
builder.Services.AddTokenizationCors(builder.Configuration);
builder.Services.AddTokenizationRateLimiting();
builder.Services.AddTokenizationIdempotency(builder.Configuration);
builder.Services.AddTokenizationOpenApi(builder.Configuration, builder.Environment);
builder.Services.AddTokenizationRequestSizeHardening(builder.Configuration);
builder.Services.AddApiHealthCheck(builder.Configuration);
builder.Services.AddTokenizationInfra(builder.Configuration);
builder.Services.AddTokenizationApplication();

var app = builder.Build();

app.UseTokenizationExceptionHandling();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ??
                      new SecurityOptions();

if (securityOptions.UseHttpsRedirection)
{
    app.UseHttpsRedirection();
}

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
app.UseCors(corsOptions.PolicyName);
app.UseAntiforgery();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSecurityHeaders();
app.UseTokenizationIdempotency();

if (app.IsSwaggerEnabled())
{
    app.UseTokenizationSwagger(builder.Configuration);
}

app.UseTokenizationLogging();
await app.InitializeTokenizationDatabaseAsync();
app.MapTokenizationEndpoints();

app.Run();

public partial class Program;
