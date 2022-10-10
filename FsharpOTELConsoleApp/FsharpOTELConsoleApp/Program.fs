﻿open System
open System.Threading.Tasks

open OpenTelemetry
open OpenTelemetry.Exporter
open OpenTelemetry.Resources
open OpenTelemetry.Trace

[<EntryPoint>]
let main (args: string[]) : int =
    let collectorEndpoint : string = "http://127.0.0.1:4317"
    let serviceName : string = "trigger-reports"

    let builder =
        Sdk.CreateTracerProviderBuilder()
            .AddSource(serviceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddOtlpExporter( fun opt ->
                opt.Endpoint <- Uri collectorEndpoint
                opt.Protocol <- OtlpExportProtocol.Grpc
                )
            .Build()
            
    let tracer = builder.GetTracer(serviceName)

    let tracerTask =
            task {
                // Track the work done in the root HTTP handler
                use span = tracer.StartActiveSpan("sleep span")
                span.SetAttribute("duration_ms", 100) |> ignore

                do! Task.Delay(100)

                return "Hello World!"
                }

    tracerTask.Wait()

    Task.Delay(5000).Wait()
    0