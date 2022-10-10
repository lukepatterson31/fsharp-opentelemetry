open System
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open OpenTelemetry.Exporter
open OpenTelemetry.Resources
open OpenTelemetry.Trace

let builder = WebApplication.CreateBuilder()

// Backend config
let serviceName = "trigger-reports"
let exporterEndpoint = "http://127.0.0.1:4007"

// Configure an exporter with:
// - endpoint 
// - service name
// - configure some automatic instrumentation
// - additional configuration as needed, e.g. headers
builder.Services.AddOpenTelemetryTracing(fun builder ->
    builder
        .AddSource(serviceName)
        .AddOtlpExporter(fun otlpOptions ->
            otlpOptions.Endpoint <- Uri exporterEndpoint
            otlpOptions.Protocol <- OtlpExportProtocol.Grpc)
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
        .AddAspNetCoreInstrumentation(fun options ->
            options.RecordException <- true)
        .AddHttpClientInstrumentation()
        //.AddSqlClientInstrumentation( fun opt ->
        //    opt.SetDbStatementForText <- true
        //    opt.RecordException <- true)
            |> ignore
        ) |> ignore

// Start a tracer scoped to the service
let tracer = TracerProvider.Default.GetTracer(serviceName)

// Add the handler to the root route
let app = builder.Build()
app.MapGet("/", Func<Task<string>>(fun () -> task {
        // Track the work done in the root HTTP handler
        use span = tracer.StartActiveSpan("sleep span")
        span.SetAttribute("duration_ms", 100) |> ignore

        do! Task.Delay(100)

        return "Hello World!"
    }) ) |> ignore

app.Run()
