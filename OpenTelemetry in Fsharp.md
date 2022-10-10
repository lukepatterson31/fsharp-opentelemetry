# Using tracing with OpenTelemetry in F#

Implement an OpenTelemetry trace exporter in an F# console and web app

(credit to Phillip Carter, blog post url: [How to use OpenTelemetry with F# | Phillip Carter's blog](https://phillipcarter.dev/posts/how-to-use-opentelemetry-with-fsharp/))


## Console app:

Create a new F# console app

```
dotnet new console --language F# -n FsharpOTELConsoleApp
```

Install the OpenTelemetry and the Exporter library 

```
dotnet add package OpenTelemetry 
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol 
```

We start by defining our OpenTelemetry collector endpoint and service name as variables, then use them to configure the TracerProvider.

```
// Set your collector endpoint here.
// Port 4317 is the default gRpc endpoint
// on OpenTelemetry collectors
let collectorEndpoint : string = "http://127.0.0.1:4317"  
let serviceName : string = "trigger-reports"  

let builder =  
	// Configure exporter with:  
	// - service name        
	// - endpoint  
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
```

Next we define `tracer` using the previously configured `builder`, create a task that we want to monitor and define `span` using `tracer` to start an active span.

We define span with the `use` keyword so it will be disposed as soon as we leave the scope of `tracerTask`.

```
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
```

This code generates a trace with one span. The span tracks the Task.Delay call in `tracerTask`

The tracer will monitor our span for the duration of the Task.Delay call, then export the trace and span to our collector.

Here's the full code sample:

```
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
        // Configure exporter with:  
        // - service name        
        // - endpoint  
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
```



