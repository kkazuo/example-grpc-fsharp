namespace GrpcServices

open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging
open Grpc.Core
open GrpcSvc

type GreeterService (logger : ILogger<GreeterService>) =
    inherit Greeter.GreeterBase ()
    override __.SayHello(request: HelloRequest, context: ServerCallContext) = task {
        logger.LogDebug("ident {@0}", context.AuthContext.PeerIdentity)
        logger.LogDebug("Name={@0}", request)
        let r = HelloReply()
        r.Message <- sprintf "hello %A" request.Name
        return r
    }
