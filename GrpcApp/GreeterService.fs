namespace GrpcServices

open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging
open Grpc.Core
open GrpcSvc

type GreeterService (loggor : ILogger) =
    inherit Greeter.GreeterBase ()
    override __.SayHello(request: HelloRequest, context: ServerCallContext) = task {
        loggor.LogDebug("Name={@0}", request)
        let r = HelloReply()
        r.Message <- sprintf "hello %A" request.Name
        return r
    }
