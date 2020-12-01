#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

#load @"./MessageTypes.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Remote
open Akka.Configuration
open System.Text.RegularExpressions

open System
open System.Collections.Generic

open MessageTypes

type BootServer = {
    BootMessage: string;
}

type ShutdownServer = {
    ShutdownMessage: string;
}

type Tweet = {
    Id: int;
    Content: string;
    CreatedTime: int;
    NextTweet: Tweet option;
}

type User = {
    Id: int;
    Handle: string;
    FirstName: string;
    LastName: string;
    TweetHead: Tweet option;
    Followers: HashSet<int>;
    FollowingTo: HashSet<int>;
}

// Remote Configuration
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                port = 5555
                hostname = localhost
            }
        }")

let system = ActorSystem.Create("Twitter", configuration)

type Server() =
    inherit Actor()
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

    // Reference to the current time
    let mutable time = 0

    override x.OnReceive (message: obj) =   
        match message with 
        | :? BootServer as bootInfo -> 
            printfn "%s" bootInfo.BootMessage
        | :? RegisterUserRequest as request ->
            try 
                let user: User = {
                    Id = users.Count + 1;
                    Handle = request.Handle;
                    FirstName = request.FirstName;
                    LastName = request.LastName;
                    TweetHead = None;
                    Followers = new HashSet<int>();
                    FollowingTo = new HashSet<int>();
                }
                
                users.Add((user.Id, user))

                handles.Add((user.Handle, user.Id))
                
                let response: RegisterUserResponse = {
                    Id = user.Id;
                    Handle = user.Handle;
                    FirstName = user.FirstName;
                    LastName = user.LastName;
                    Success = true;
                }
                x.Sender.Tell response
            with
                :? System.ArgumentException -> 
                    let response: RegisterUserResponse = {
                        Id = -1;
                        Handle = request.Handle;
                        FirstName = "";
                        LastName = "";
                        Success = false;
                    }
                    x.Sender.Tell response
        | :? FollowUserRequest as request -> 
            users.[request.FollowerId].FollowingTo.Add(request.FolloweeId) |> ignore
            users.[request.FolloweeId].Followers.Add(request.FollowerId) |> ignore
            let response: FollowUserResponse = { Success = true; }
            x.Sender.Tell response
        | :? UnfollowUserRequest as request ->
            users.[request.FollowerId].FollowingTo.Remove(request.FolloweeId) |> ignore
            users.[request.FolloweeId].Followers.Remove(request.FollowerId) |> ignore
            let response: UnfollowUserResponse = { Success = true; }
            x.Sender.Tell response
        | :? PostTweetRequest as request -> 
            printfn "Post tweet: %A" request
            time <- time + 1
            let tweet: Tweet = {
                Id = tweets.Count + 1;
                Content = request.Content;
                CreatedTime = time;
                NextTweet = None;
            }
            tweets.Add((tweet.Id, tweet))

            // Parse user mentions from the tweet content and update relevant dict
            let mentionPattern = "/(?<!\w)@(\w+)/" // takes care of the email address mention
            let mentiondMatch = Regex.Match(mentionPattern, request.Content)
            
            for group in mentiondMatch.Groups do
                let mutable handle = group.Value

                if group.Success then 
                    let userId = Convert.ToInt32(handles.TryGetValue(handle))
                    if mentions.ContainsKey(userId) then
                        mentions.[userId].Add(tweet.Id)
                    else 
                        let tweetIdList = new List<int>()
                        tweetIdList.Add(tweet.Id)
                        mentions.Add(userId, tweetIdList)

            // Parse hashtags from the tweet content and update relevant dict
            let tagPattern = "/(^|\B)#(?![0-9_]+\b)([a-zA-Z0-9_]{1,30})(\b|\r)/g"
            let tagMatch = Regex.Match(tagPattern, request.Content)

            for group in tagMatch.Groups do
                let mutable tag = group.Value

                if group.Success then 
                    if hashtags.ContainsKey(tag) then
                        hashtags.[tag].Add(tweet.Id)
                    else
                        let tweetIdList = new List<int>()
                        tweetIdList.Add(tweet.Id)
                        hashtags.Add(tag, tweetIdList)
            // Send response
            let tweetSuccess = tweets.ContainsKey(tweet.Id)

            let response: PostTweetResponse = {
                UserId = request.UserId;
                TweetId = tweet.Id;
                Content = tweets.[tweet.Id].Content;
                Success = tweetSuccess;
            }

            x.Sender.Tell response

        | :? PrintInfo as request -> 
            let user = users.[request.Id]
            printfn "%d | %s | %s | %s | %d | %d" user.Id user.Handle user.FirstName user.LastName user.Followers.Count user.FollowingTo.Count
        | _ -> ()

let server = system.ActorOf(Props(typedefof<Server>), "Server")

let (task:Async<ShutdownServer>) = (server <? { BootMessage = "Server is running!"; })

let response = Async.RunSynchronously (task)
printfn "%A" response

server.Tell(PoisonPill.Instance);
