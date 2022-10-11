open System
open System.Threading.Tasks

open OpenTelemetry
open OpenTelemetry.Exporter
open OpenTelemetry.Resources
open OpenTelemetry.Trace

[<EntryPoint>]
let main (args: string[]) : int =
    
    // Set your collector endpoint here
    // I use port 4317 which is the default gRpc endpoint 
    let collectorEndpoint : string = "http://127.0.0.1:4317"
    let serviceName : string = "trigger-reports"

    let builder =
        // Add the service name as a source to the TracerProvider
        // Configure the exporter with:
        // - endpoint of your OpenTelemetry collector
        // - protocol (gRpc or Http)
        // - additional configuration as needed, e.g. headers
        Sdk.CreateTracerProviderBuilder()
            .AddSource(serviceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddOtlpExporter( fun opt ->
                opt.Endpoint <- Uri collectorEndpoint
                opt.Protocol <- OtlpExportProtocol.Grpc                
                )
            .Build()
            
    let tracer = builder.GetTracer(serviceName)

    // Task simulating asynchronous code/external call
    let tracerTask =
            task {
                // Start a span to monitor our "call"
                use span = tracer.StartActiveSpan("sleep span")
                // Set an attribute for our span
                span.SetAttribute("duration_ms", 100) |> ignore

                do! Task.Delay(100)

                return "Hello World!"
                }

    tracerTask.Wait()

    // Wait here as the TracerProvider can be disposed
    // before we send the trace to our OpenTelemetry collector
    Task.Delay(5000).Wait()
    0