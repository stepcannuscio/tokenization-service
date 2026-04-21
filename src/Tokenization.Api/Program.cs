using Tokenization.Api.Config;
using Tokenization.Api.Logging.Config;

var builder = WebApplication.CreateBuilder(args);

// Configure services.
builder.AddTokenizationLogging();
builder.Services.AddTokenizationApi(builder.Configuration)
    .AddControllers()
    .AddVersioning()
    .AddProblemDetails()
    .AddAuthentication()
    .AddSecurity()
    .AddCors()
    .AddRateLimiting()
    .AddIdempotency()
    .AddOpenApi()
    .AddRequestSizeLimits()
    .AddHealthChecks()
    .AddInfra()
    .AddApplication();

var app = builder.Build();

// Configure middleware pipeline.
app.UseTokenizationApi(builder.Configuration)
    .UseExceptionHandling()
    .UseHsts()
    .UseDefaultSecurity()
    .UseSwagger()
    .UseLogging()
    .MapEndpoints();

app.Run();

// Make Program class public for integration tests
public partial class Program;