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
        
        let rec loop() = 
            actor {
                let! message = mailbox.Receive()
                printfn "%A" message
                match box message with 
                | :? string as request ->
                    printfn "Client request: %A" request
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

                            let response = 
                                Messages.REGISTER_USER_RESPONSE + "|" + 
                                (userInstance.Id |> string) + "|" +
                                userInstance.Handle + "|" + 
                                userInstance.FirstName + "|" +
                                userInstance.LastName + "|true"
                            mailbox.Sender() <! response
                        with
                            :? System.Exception -> 
                            let response = 
                                Messages.REGISTER_USER_RESPONSE + "|0|" + 
                                data.[1] + "|||false"
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