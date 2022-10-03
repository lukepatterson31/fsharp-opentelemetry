open System
open System.Diagnostics
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open OpenTelemetry
open OpenTelemetry.Resources
open OpenTelemetry.Trace

[<EntryPoint>]
let main (args: string[]) =
    let serviceName = "trigger-reports"
    let builder = WebApplication.CreateBuilder(args)
    builder.Services

    let app = builder.Build()

    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore

    app.Run()
    0
