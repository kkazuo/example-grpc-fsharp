module GrpcApp.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.EndpointRouting
open GrpcServices

// ---------------------------------
// Web app
// ---------------------------------

let endpoints =
    [
        GET => route "/" (setHttpHeader "content-type" "text/plain" >=> text "world")
    ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (policy : CorsPolicyBuilder) =
    policy
        .WithOrigins("http://localhost:8080")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let configureEndpoints (route : IEndpointRouteBuilder) =
    route.MapGrpcService<GreeterService>() |> ignore
    route.MapGiraffeEndpoints(endpoints)

open Serilog
open Serilog.Events
open Serilog.Exceptions

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.EnvironmentName with
    | "Development" -> app.UseDeveloperExceptionPage()
    | _ -> app.UseGiraffeErrorHandler(errorHandler))
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseSerilogRequestLogging()
        .UseRouting()
        .UseEndpoints(Action<IEndpointRouteBuilder> configureEndpoints)
        |> ignore

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddGrpc()    |> ignore

let configureLogging (logging : ILoggingBuilder) =
    logging
        .AddFilter(fun l -> l.Equals LogLevel.Error)
        .AddConsole()
        .AddSerilog(Log.Logger, false)
    |> ignore

let configureSerilog () =
    let fmt = "{Timestamp:MMdd HHmm ss.fffff} | {Level:u3} | {Message:l} | {Properties}{NewLine}{Exception}"

    let config = LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.HttpsPolicy", LogEventLevel.Error)
                    .MinimumLevel.Override("Grpc", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails()
                    .WriteTo.Console(LogEventLevel.Verbose, fmt)
    Log.Logger <- config.CreateLogger()

let configureKestrel (options : KestrelServerOptions) =
    options.ListenAnyIP(5000, fun o -> o.Protocols <- HttpProtocols.Http2)

let configureWebHost (webHost : IWebHostBuilder) =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    webHost
        .UseContentRoot(contentRoot)
        .UseWebRoot(webRoot)
        .ConfigureKestrel(Action<KestrelServerOptions> configureKestrel)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(Action<IServiceCollection> configureServices)
        .ConfigureLogging(Action<ILoggingBuilder> configureLogging)
    |> ignore

let runClient () =
    use channel = Grpc.Net.Client.GrpcChannel.ForAddress "http://localhost:5000"
    let client = GrpcSvc.Greeter.GreeterClient channel

    while true do
        let req = GrpcSvc.HelloRequest()
        req.Name <- "test"
        let res = client.SayHello(req)
        printfn "%s" res.Message
        ()

[<EntryPoint>]
let main args =
    configureSerilog()

    if args.Length > 0 then
        runClient() //|> Async.AwaitTask |> Async.RunSynchronously

    try
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(Action<IWebHostBuilder> configureWebHost)
            .Build()
            .Run()
    finally
        Log.CloseAndFlush()
    0
