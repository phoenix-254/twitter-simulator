#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

#load @"./MessageType.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System
open System.Collections.Generic

open MessageType

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
                    hostname = localhost
                }
            }
        }")

let system = ActorSystem.Create("TwitterEngine", configuration)

let Server (mailbox: Actor<_>) = 
    spawn system "Server"
    <| fun mailbox ->
        // User Id -> User Instance mapping
        let users = new Dictionary<int, MessageType.User>()

        // Tweet Id -> Tweet Instance mapping
        let tweets = new Dictionary<int, MessageType.Tweet>()

        // User Handle -> User Id mapping
        let handles = new Dictionary<string, int>()

        // Hashtag -> List<Tweet Id> mapping, for searching tweets having a specific hashtag
        let hashtags = new Dictionary<string, List<int>>()

        // User Id -> List<Tweet Id> mapping, for searching tweets where user is mentioned
        let mentions = new Dictionary<int, List<int>>()

        let rec loop() = actor {
            let! message = mailbox.Receive()

            match message with
                | MessageType.RegisterUserRequest request ->
                    printfn "register user request: %A" message
                    let userInstance: MessageType.User = {
                        Id = users.Count + 1;
                        Handle = request.Handle;
                        FirstName = request.FirstName;
                        LastName = request.LastName;
                        TweetHead = None;
                        FollowingTo = new HashSet<int>();
                        HashtagsFollowed = new HashSet<string>();
                    }

                    users.Add((userInstance.Id, userInstance))

                    let response: MessageType.RegisterUserResponse = {
                        Id = userInstance.Id;
                        Handle = userInstance.Handle;
                        FirstName = userInstance.FirstName;
                        LastName = userInstance.LastName;
                        Success = true;
                    }
                    mailbox.Sender() <! MessageType.RegisterUserResponse response
                | _ -> 
                    printfn "Received message - %A from Client." message
                
            return! loop()
        }
        loop()

printfn "Server is running!"

Console.ReadLine() |> ignore

system.Terminate() |> ignore
