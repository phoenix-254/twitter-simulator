namespace TwitterClient

#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System

module TwitterClient = 
    // Remote configuration
    let configuration = 
        ConfigurationFactory.ParseString(
            @"akka {
                log-config-on-start : off
                stdout-loglevel : DEBUG
                loglevel : ERROR
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                    debug : {
                        receive : off
                        autoreceive : off
                        lifecycle : off
                        event-stream : off
                        unhandled : off
                    }
                }
                remote {
                    helios.tcp {
                        port = 8888
                        hostname = ""192.168.0.193""
                    }
                }
            }")

    let system = ActorSystem.Create ("TwitterClient", configuration)

    let Client (mailbox: Actor<_>) = 
        let rec loop () = actor {
            let! message = mailbox.Receive()

            match message with
                | _ -> 
                    let serverNode = system.ActorSelection("akka.tcp://TwitterEngine@192.168.0.193:7777/user/Server")
                    serverNode <! "Hello Server!"
                    printfn "Received message - %A from Simulator." message

            return! loop()

        }
        loop ()
    
    spawn system "Client" Client
    
    let client = system.ActorSelection("akka.tcp://TwitterClient@192.168.0.193:8888/user/Client")

    let hello() = 
        client <! "Hello Client!"
        Console.ReadLine() |> ignore
