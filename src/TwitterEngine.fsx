#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

#load @"./Models.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System
open System.Collections.Generic

open Models

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
    // User Id -> User Instance mapping
    let users = new Dictionary<int, Models.User>()

    // Tweet Id -> Tweet Instance mapping
    let tweets = new Dictionary<int, Models.Tweet>()

    // User Handle -> User Id mapping
    let handles = new Dictionary<string, int>()

    // Hashtag -> List<Tweet Id> mapping, for searching tweets having a specific hashtag
    let hashtags = new Dictionary<string, List<int>>()

    // User Id -> List<Tweet Id> mapping, for searching tweets where user is mentioned
    let mentions = new Dictionary<int, List<int>>()

    let rec loop() = actor {
        let! message = mailbox.Receive()

        printfn "received message %A from client" message

        match message with
            | Models.MessageType.RegisterUserRequest request ->
                printfn "Registering user %A" request
                let userInstance: Models.User = {
                    Id = users.Count + 1;
                    Handle = request.Handle;
                    FirstName = request.FirstName;
                    LastName = request.LastName;
                    TweetHead = None;
                    FollowingTo = new HashSet<int>();
                    HashtagsFollowed = new HashSet<string>();
                }

                users.Add((userInstance.Id, userInstance))

                let response: Models.RegisterUserResponse = {
                    Id = userInstance.Id;
                    Handle = userInstance.Handle;
                    FirstName = userInstance.FirstName;
                    LastName = userInstance.LastName;
                    Success = true;
                }
                mailbox.Sender() <! Models.MessageType.RegisterUserResponse response
            | Models.MessageType.FollowUserRequest request -> 
                printfn "User %d follows user %d" request.FollowerId request.FolloweeId
            | Models.MessageType.UnfollowUserRequest request ->
                printfn "User %d follows user %d" request.FollowerId request.FolloweeId
            | Models.MessageType.PostTweetRequest request ->
                printfn "User %d posting tweet: %s" request.UserId request.TweetContent
            | Models.MessageType.RetweetRequest request ->
                printfn "User %d retweeting: %d" request.UserId request.TweetId
            | Models.MessageType.GetFeedRequest request -> 
                printfn "Getting feed for user: %d" request.UserId
            | Models.MessageType.GetMentionTweetsRequest request ->
                printfn "Getting tweets where I'm mentioned %A" request
            | Models.MessageType.GetHashtagTweetsRequest request ->
                printfn "Getting tweets with hashtags: %A" request
            | Models.MessageType.GetFollowersTweetsRequest request ->
                printfn "Getting tweets from my followers: %A" request
            | _ -> 
                printfn "Received message - %A from Client." message
            
        return! loop()
    }
    loop()

spawn system "Server" Server

printfn "Server is running!"

Console.ReadLine() |> ignore

system.Terminate() |> ignore
