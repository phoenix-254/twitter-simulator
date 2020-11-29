namespace TwitterClient

#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#load @"./MessageType.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System
open System.Collections.Generic

open MessageType

module TwitterClient = 
    // Remote configuration
    (*let configuration = 
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
                        hostname = localhost
                    }
                }
            }")*)

    let system = ActorSystem.Create "TwitterClient"

    let Server (mailbox: Actor<_>) = 
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

                    handles.Add((request.Handle, userInstance.Id))
    
                    let response: MessageType.RegisterUserResponse = {
                        Id = userInstance.Id;
                        Handle = userInstance.Handle;
                        FirstName = userInstance.FirstName;
                        LastName = userInstance.LastName;
                        Success = true;
                    }
                    mailbox.Sender() <! MessageType.RegisterUserResponse response
                | MessageType.FollowUserRequest request -> 
                    printfn "Follow user request : %A" request
                    let success = users.[request.FollowerId].FollowingTo.Add(request.FolloweeId)
                    let response: MessageType.FollowUserResponse = {
                        Success = success;
                    }
                    mailbox.Sender() <! MessageType.FollowUserResponse response
                | MessageType.UnfollowUserRequest request ->
                    let success = users.[request.FollowerId].FollowingTo.Remove(request.FolloweeId)
                    let response: MessageType.UnfollowUserResponse = {
                        Success = success;
                    }
                    mailbox.Sender() <! MessageType.UnfollowUserResponse response
                | MessageType.PostTweetRequest request ->
                    printfn "asdf"
                | MessageType.RetweetRequest request ->
                    printfn "asdf"
                | MessageType.GetFeedRequest request ->
                    printfn "asdf"
                | MessageType.GetMentionTweetsRequest request ->
                    printfn "asdf"
                | MessageType.GetHashtagTweetsRequest request ->
                    printfn "asdf"
                | MessageType.GetFollowersTweetsRequest request ->
                    printfn "asdf"
                | MessageType.PrintInfo ->
                    if users.Count > 0 then
                        printfn "%A" users
                    
                    if tweets.Count > 0 then
                        printfn "%A" tweets
                    
                    if handles.Count > 0 then
                        printfn "%A" handles
                    
                    if hashtags.Count > 0 then
                        printfn "%A" hashtags
                    
                    if mentions.Count > 0 then
                        printfn "%A" mentions
                | _ -> 
                    printfn "Invalid message! %A" message
                    
            return! loop()
        }
        loop()

    // Global server node reference
    let server = spawn system "Server" Server

    // Client nodes list
    let clients = new List<IActorRef>()

    // Global supervisor instance
    let mutable supervisor: IActorRef = null

    // Returns number of followee based on the id of the user
    let getFolloweeCount (id: int) = 
        if id < 100 then 8
        else 25

    let Client (mailbox: Actor<_>) = 
        let mutable userId: int = 0
        let mutable handle: string = ""
        let mutable firstName: string = ""
        let mutable lastName: string = ""

        let mutable followerCount = 0
        let mutable generatedFollowerCount = 0

        let random = Random()

        // Returns the number of followers this user should have based on the Zipf distribution
        let getFollowersCount () = 
            5

        let getRandomFollwer () = 
            let follower: MessageType.FollowUserRequest = {
                FollowerId = random.Next(1, clients.Count + 1);
                FolloweeId = userId;
            }
            follower

        let rec loop () = actor {
            let! message = mailbox.Receive()

            match message with
                | MessageType.RegisterUserRequest request -> 
                    server <! MessageType.RegisterUserRequest request
                | MessageType.RegisterUserResponse response -> 
                    if response.Success then
                        userId <- response.Id
                        handle <- response.Handle
                        firstName <- response.FirstName
                        lastName <- response.LastName

                        supervisor <! MessageType.GenerateUserSuccess
                    else
                        printfn "User registeration failed!"
                | MessageType.GenerateFollowers ->
                    followerCount <- getFollowersCount()
                    
                    [1 .. followerCount]
                    |> List.iter(fun i ->   server <! MessageType.FollowUserRequest (getRandomFollwer()))
                    |> ignore

                | MessageType.FollowUserResponse response ->
                    if response.Success then
                        generatedFollowerCount <- generatedFollowerCount + 1
                        if generatedFollowerCount = followerCount then
                            supervisor <! MessageType.GenerateFollowersSuccess
                    else 
                        server <! MessageType.FollowUserRequest (getRandomFollwer())
                | _ -> 
                    printfn "Invalid message! %A" message

            return! loop()
        }
        loop ()

    let Supervisor (mailbox: Actor<_>) = 
        let mutable numberOfUsers: int = 0
        let mutable numberOfUsersCreated: int = 0
        let mutable numberOfUsersWithFollowers: int = 0

        let random = Random()
        
        let upperCase = [|'A' .. 'Z'|]
        let lowerCase = [|'a' .. 'z'|]
        let generateRandomName (): string = 
            let len = random.Next(10)
            
            let mutable name: string = ""
            name <- name + (upperCase.[random.Next(26)] |> string)
            
            [1 .. (len-1)]
            |> List.iter(fun i ->   name <- name + (lowerCase.[random.Next(26)] |> string))
            |> ignore

            name

        let rec loop () = actor {
            let! message = mailbox.Receive()

            match message with
                | MessageType.InitClient input ->
                    numberOfUsers <- input.NumberOfUsers
                | MessageType.GenerateUsers ->
                    [1 .. numberOfUsers]
                    |> List.iter(fun i ->   let name = "Client" + (i |> string)
                                            let client = spawn system name Client
                                            clients.Add(client)
                                            
                                            let request: MessageType.RegisterUserRequest = {
                                                Handle = ("User_" + (i |> string));
                                                FirstName = generateRandomName();
                                                LastName = generateRandomName();
                                            }
                                            client <! MessageType.RegisterUserRequest request)
                    |> ignore
                | MessageType.GenerateUserSuccess ->
                    numberOfUsersCreated <- numberOfUsersCreated + 1
                    if numberOfUsersCreated = numberOfUsers then
                        server <! MessageType.PrintInfo

                        [1 .. numberOfUsers]
                        |> List.iter(fun i ->   clients.[i-1] <! MessageType.GenerateFollowers)
                        |> ignore
                | MessageType.GenerateFollowersSuccess -> 
                    numberOfUsersWithFollowers <- numberOfUsersWithFollowers + 1
                    if numberOfUsersWithFollowers = numberOfUsers then
                        server <! MessageType.PrintInfo
                | _ ->
                    printfn "Invalid message %A at supervisor!" message 
            return! loop()
        }

        loop()
    
    supervisor <- spawn system "Supervisor" Supervisor

    let registerUsers (numberOfUsers: int) = 
        let input: MessageType.InitClient = {
            NumberOfUsers = numberOfUsers;
        }
        supervisor <! MessageType.InitClient input
        
        let task = supervisor <? MessageType.GenerateUsers
        Async.RunSynchronously(task) |> ignore
