#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#load @"./Messages.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System
open System.Collections.Generic

open Messages

type Tweet = {
    Id: int;
    Content: string;
    CreatedTime: int;
    NextTweet: Tweet;
}

type User = {
    Id: int;
    Handle: string;
    FirstName: string;
    LastName: string;
    TweetHead: Tweet option;
    FollowingTo: HashSet<int>;
    HashtagsFollowed: HashSet<string>;
}

// Remote Configuration
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                port = 5555
                hostname = ""127.0.0.1""
            }
        }")

let system = System.create "System" configuration

let Server = 
    spawn system "server"
    <| fun mailbox -> 
        // User Id -> User Instance mapping
        let users = new Dictionary<int, User>()

        // Tweet Id -> Tweet Instance mapping
        let tweets = new Dictionary<int, Tweet>()
    
        // User Handle -> User Id mapping
        let handles = new Dictionary<string, int>()
    
        // Hashtag -> List<Tweet Id> mapping, for searching tweets having a specific hashtag
        let hashtags = new Dictionary<string, List<int>>()
    
        // User Id -> List<Tweet Id> mapping, for searching tweets where user is mentioned
        let mentions = new Dictionary<int, List<int>>()
        
        let rec loop() = 
            actor {
                let! message = mailbox.Receive()
                match box message with 
                | :? string as request ->
                    let data = request.Split "|"
                    
                    match data.[0] with
                    | Messages.REGISTER_USER_REQUEST ->
                        try 
                            let userInstance: User = {
                                Id = users.Count + 1;
                                Handle = data.[1];
                                FirstName = data.[2];
                                LastName = data.[3];
                                TweetHead = None;
                                FollowingTo = new HashSet<int>();
                                HashtagsFollowed = new HashSet<string>();
                            }
                            
                            users.Add((userInstance.Id, userInstance))

                            handles.Add((userInstance.Handle, userInstance.Id))

                            let response = 
                                Messages.REGISTER_USER_RESPONSE + "|" + 
                                (userInstance.Id |> string) + "|" +
                                userInstance.Handle + "|" + 
                                userInstance.FirstName + "|" +
                                userInstance.LastName + "|true"
                            mailbox.Sender() <! response
                        with
                            :? System.Exception -> 
                            let response = Messages.REGISTER_USER_RESPONSE + "|0|" + data.[1] + "|||false"
                            mailbox.Sender() <! response
                    | Messages.FOLLOW_USER_REQUEST ->
                        let followerId = data.[1] |> int
                        let followeeId = data.[2] |> int
                        users.[followerId].FollowingTo.Add(followeeId) |> ignore
                        let response = Messages.FOLLOW_USER_RESPONSE + "|true"
                        mailbox.Sender() <! response
                    | Messages.UNFOLLOW_USER_REQUEST ->
                        let followerId = data.[1] |> int
                        let followeeId = data.[2] |> int
                        users.[followerId].FollowingTo.Remove(followeeId) |> ignore
                        let response = Messages.UNFOLLOW_USER_RESPONSE + "|true"
                        mailbox.Sender() <! response
                    | _ -> printfn "Invalid message %s at server." request
                | _ -> 
                    let failureMessage = "Invalid message at Server!"
                    failwith failureMessage
                
                return! loop()
            }
        loop()

printfn "Server is up and running!"

Console.ReadLine() |> ignore

system.Terminate() |> ignore