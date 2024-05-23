using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using SPM.Controllers;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure tracing with OpenTelemetry and export to Jaeger
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(builder.Configuration["OtlpExporterEndpoint"]); // Update with your OTLP exporter endpoint
    })
    .Build();

// Ensure tracer provider is set globally
Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());
Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

// Add services to the container
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true; // The logged messages should include formatted messages
    options.IncludeScopes = true; // Scopes can provide additional context for log messages
    options.AddOtlpExporter(); // OTLP is a protocol used to export telemetry data
});

builder.Services.AddControllers();
builder.Services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.All);
builder.Services.AddSingleton(new Errors(builder.Configuration.GetValue<double>("ErrorRate")));

builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
{
    // Filter out instrumentation of the Prometheus scraping endpoint.
    options.Filter = ctx => ctx.Request.Path != "/metrics";
});

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation() // Instrument ASP.NET Core for tracing HTTP requests and middleware.
        .AddHttpClientInstrumentation() // Instrument HTTP client for tracing outgoing requests.
        .AddOtlpExporter()) // Export traces using OTLP (OpenTelemetry Protocol).
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation() // Instrument ASP.NET Core for collecting HTTP-related metrics.
        .AddHttpClientInstrumentation() // Instrument HTTP client for collecting metrics.
        .AddRuntimeInstrumentation() // Instrument for collecting runtime metrics like CPU and memory.
        .AddProcessInstrumentation() // Instrument for collecting process-related metrics.
        .AddPrometheusExporter()); // Export metrics to Prometheus.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseHttpLogging();
app.UseDeveloperExceptionPage();
app.UseAuthorization();
app.MapControllers();
app.Run();
