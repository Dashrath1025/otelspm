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

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true; //the logged messages should include formatted messages
        options.IncludeScopes = true; //Scopes can provide additional context for log messages
        var resBuilder = ResourceBuilder.CreateDefault();
      //  var serviceName = builder.Configuration["ServiceName"]!;
        var serviceName = builder.Configuration["ServiceName"]!;
        resBuilder.AddService(serviceName);
        options.SetResourceBuilder(resBuilder)
        .AddOtlpExporter(options =>
         {
             options.Endpoint = new Uri(Environment.GetEnvironmentVariable("OtlpExporterEndpoint") ?? "http://localhost:4317"); // Update with your OTLP exporter endpoint
         });

    });

// Add services to the container


builder.Services.AddControllers();
builder.Services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.All);
builder.Services.AddSingleton(new Errors(builder.Configuration.GetValue<double>("ErrorRate")));

builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
{
    // Filter out instrumentation of the Prometheus scraping endpoint.
    options.Filter = ctx => ctx.Request.Path != "/metrics";
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(b =>
    {
        b.AddService(builder.Configuration["ServiceName"]!);
    })
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
