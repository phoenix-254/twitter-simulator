#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System

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
                    port = 7777
                    hostname = ""192.168.0.193""
                }
            }
        }")

let system = ActorSystem.Create("TwitterEngine", configuration)

let Server (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with
            | _ -> printfn "Received message - %A from Client." message
            
        return! loop()
    }
    loop()

spawn system "Server" Server

printfn "Server is running!"

Console.ReadLine() |> ignore
