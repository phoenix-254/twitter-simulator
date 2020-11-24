#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

#load @"./Models.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System

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
    let users: Map<int, Models.User> = Map.empty

    // Tweet Id -> Tweet Instance mapping
    let tweets: Map<int, Models.Tweet> = Map.empty

    // User Handle -> User Id mapping
    let handles: Map<string, int> = Map.empty

    // Hashtag -> List<Tweet Id> mapping, for searching tweets having a specific hashtag
    let hashtags: Map<string, List<int>> = Map.empty

    // User Id -> List<Tweet Id> mapping, for searching tweets where user is mentioned
    let mentions: Map<int, List<int>> = Map.empty

    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with
            | Models.ServerRequestType.RegisterUser request ->
                printfn "Registering user %A" request
            | Models.ServerRequestType.FollowUser request -> 
                printfn "User %d follows user %d" request.FollowerId request.FolloweeId
            | Models.ServerRequestType.UnfollowUser request ->
                printfn "User %d follows user %d" request.FollowerId request.FolloweeId
            | Models.ServerRequestType.PostTweet request ->
                printfn "User %d posting tweet: %s" request.UserId request.TweetContent
            | Models.ServerRequestType.Retweet request ->
                printfn "User %d retweeting: %d" request.UserId request.TweetId
            | Models.ServerRequestType.GetFeed request -> 
                printfn "Getting feed for user: %d" request.UserId
            | Models.ServerRequestType.GetMentionTweets request ->
                printfn "Getting tweets where I'm mentioned %A" request
            | Models.ServerRequestType.GetHashtagTweets request ->
                printfn "Getting tweets with hashtags: %A" request
            | Models.ServerRequestType.GetFollowersTweets request ->
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
