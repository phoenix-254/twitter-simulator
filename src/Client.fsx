namespace Client

#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#load @"./Messages.fsx"

module Client = 
    open Akka.Actor
    open Akka.FSharp
    open Akka.Configuration

    open System
    open System.Collections.Generic

    open Messages

    // Remote Configuration
    let configuration = 
        ConfigurationFactory.ParseString(
            @"akka {
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                    deployment {
                        /server {
                            remote = ""akka.tcp://System@127.0.0.1:5555""
                        }
                    }
                }
                remote.helios.tcp {
                    port = 0
                    hostname = ""127.0.0.1""
                }
            }")

    let system = ActorSystem.Create("System", configuration)
    
    let server = system.ActorSelection("akka.tcp://System@127.0.0.1:5555/user/server")

    // Total number of registered users
    let mutable totalUsers = 0

    // User categories
    let mutable starCelebrityCount: int = 0
    let starCelebrityPercent: float = 0.05
    let starCelebrityFollowersRange = [|65.0; 90.0;|]

    let mutable celebrityCount: int = 0
    let celebrityPercent: float = 0.4
    let celebrityFollowersRange = [|45.0; 72.0;|]
    
    let mutable influencerCount: int = 0
    let influencerPercent: float = 8.4
    let influencerFollowersRange = [|11.0; 54.0;|]
    
    let mutable publicFigureCount: int = 0
    let publicFigurePercent: float = 14.7
    let publicFigureFollowersRange = [|1.0; 23.0;|]
    
    let mutable commonMenCount: int = 0
    let commonMenFollowersRange = [|0.0; 1.5;|]

    // Followers distribution map
    // [Min, Max] of UserId -> [Min, Max] of followers count
    let followersDistribution = new Dictionary<int[], int[]>()
    
    // Global supervisor actor reference
    let mutable supervisor: IActorRef = null
    
    // Client nodes list
    let clients = new List<IActorRef>()

    type InitInfo = { NumberOfUsers: int; }
    type RegisterUser = { Id: int; }
    type FollowUser = { Id: int; }
    
    let percentOf(number: int, percent: float) = 
        ((number |> float) * percent / 100.0) |> int

    let calculateUserCategoryCount() = 
        starCelebrityCount <- percentOf(totalUsers, starCelebrityPercent)
        celebrityCount <- percentOf(totalUsers, celebrityPercent)
        influencerCount <- percentOf(totalUsers, influencerPercent)
        publicFigureCount <- percentOf(totalUsers, publicFigurePercent)
        commonMenCount <- totalUsers - (starCelebrityCount + celebrityCount + influencerCount + publicFigureCount)

    let calculateFollowersDistribution() = 
        let mutable prevEnd = 1
        followersDistribution.Add(([|prevEnd; prevEnd + starCelebrityCount;|], 
            [|percentOf(totalUsers, starCelebrityFollowersRange.[0]); percentOf(totalUsers, starCelebrityFollowersRange.[1]);|]))
        prevEnd <- prevEnd + starCelebrityCount

        followersDistribution.Add(([|prevEnd; prevEnd + celebrityCount;|], 
            [|percentOf(totalUsers, celebrityFollowersRange.[0]); percentOf(totalUsers, celebrityFollowersRange.[1]);|]))
        prevEnd <- prevEnd + celebrityCount

        followersDistribution.Add(([|prevEnd; prevEnd + influencerCount;|], 
            [|percentOf(totalUsers, influencerFollowersRange.[0]); percentOf(totalUsers, influencerFollowersRange.[1]);|]))
        prevEnd <- prevEnd + influencerCount

        followersDistribution.Add(([|prevEnd; prevEnd + publicFigureCount;|], 
            [|percentOf(totalUsers, publicFigureFollowersRange.[0]); percentOf(totalUsers, publicFigureFollowersRange.[1]);|]))
        prevEnd <- prevEnd + publicFigureCount

        followersDistribution.Add(([|prevEnd; totalUsers + 1;|], 
            [|percentOf(totalUsers, commonMenFollowersRange.[0]); percentOf(totalUsers, commonMenFollowersRange.[1]);|]))

    let Client (mailbox: Actor<_>) =
        let mutable userId: int = 0
        let mutable handle: string = ""
        let mutable firstName: string = ""
        let mutable lastName: string = ""

        let random = Random()
        
        let upperCase = [|'A' .. 'Z'|]
        let lowerCase = [|'a' .. 'z'|]
        let generateRandomName(): string = 
            let len = random.Next(10)
            
            let mutable name: string = ""
            name <- name + (upperCase.[random.Next(26)] |> string)
            
            [1 .. (len-1)]
            |> List.iter(fun i ->   name <- name + (lowerCase.[random.Next(26)] |> string))
            |> ignore
    
            name

        let rec getNumberOfFollowers() =  
            let mutable count = 0
            for entry in followersDistribution do
                if (userId >= entry.Key.[0] && userId < entry.Key.[1]) then 
                    count <- random.Next(entry.Value.[0], entry.Value.[1])
            count

        let getRandomFollowerPair() = 
            [|random.Next(1, totalUsers + 1); userId;|]

        let rec loop() = actor{
            let! message = mailbox.Receive();
    
            match box message with 
                | :? RegisterUser as input -> 
                    let request = 
                        Messages.REGISTER_USER_REQUEST + "|" + 
                        ("User" + (input.Id |> string)) + "|" +
                        (generateRandomName()) + "|" +
                        (generateRandomName())
                    server <! request
                | :? FollowUser as input -> 
                    let numberOfFollowers = getNumberOfFollowers()
                    [1 .. numberOfFollowers]
                    |> List.iter(fun i ->   let pair = getRandomFollowerPair()
                                            let request = Messages.FOLLOW_USER_REQUEST + "|" + (pair.[0] |> string) + "|" + (pair.[1] |> string)
                                            server <! request)
                | :? string as response -> 
                    let data = response.Split "|"
                    match data.[0] with
                    | Messages.REGISTER_USER_RESPONSE -> 
                        printfn "Response from server: %s" response
                        match data.[5] with
                        | True -> 
                            userId <- (data.[1] |> int)
                            handle <- data.[2]
                            firstName <- data.[3]
                            lastName <- data.[4]
                            supervisor <! Messages.REGISTER_USER_SUCCESS
                        | False ->
                            printfn "User with handle %s failed to register." data.[2]
                    | Messages.FOLLOW_USER_RESPONSE ->
                        match data.[1] with
                        | True -> supervisor <! Messages.FOLLOW_USER_SUCCESS
                        | False -> printfn "Follow request failed."
                    | _ -> printfn "Invalid message %s at client %d." response userId
                | _ -> 
                    let failureMessage = "Invalid message at Client!"
                    failwith failureMessage
            return! loop()
        }            
        loop()
    
    let Supervisor (mailbox: Actor<_>) =
        let mutable numberOfUsersCreated: int = 0
        
        let mutable followerCount: int = 0
    
        let mutable parent: IActorRef = null
    
        let rec loop()= actor{
            let! message = mailbox.Receive();
            
            match box message with 
            | :? InitInfo as input -> 
                totalUsers <- input.NumberOfUsers
                parent <- mailbox.Sender()

                calculateUserCategoryCount()
                calculateFollowersDistribution()

                printfn "starceleb : %d" starCelebrityCount
                printfn "celeb : %d" celebrityCount
                printfn "influencers : %d" influencerCount
                printfn "publicFig : %d" publicFigureCount
                printfn "commonMen : %d" commonMenCount
                
                printfn "Followers distribution"
                for entry in followersDistribution do
                    printfn "%A : %A" entry.Key entry.Value
            | :? string as request -> 
                let data = request.Split "|"
                match data.[0] with
                | Messages.REGISTER_USERS ->
                    [1 .. totalUsers]
                    |> List.iter(fun i ->   let name = "Client" + (i |> string)
                                            let client = spawn system name Client
                                            clients.Add(client)
                                            let registerUser: RegisterUser = { Id = i; }
                                            client <! registerUser)
                    |> ignore
                | Messages.REGISTER_USER_SUCCESS ->
                    numberOfUsersCreated <- numberOfUsersCreated + 1
                    if numberOfUsersCreated = totalUsers then
                        [1 .. totalUsers]
                        |> List.iter(fun i ->   let followUser: FollowUser = { Id = i; }
                                                clients.[i-1] <! followUser)
                        |> ignore
                | Messages.FOLLOW_USER_SUCCESS ->
                    followerCount <- followerCount + 1
                    printf "%d|" followerCount
                | _ -> printfn "Invalid message %s at supervisor." request
            | _ -> 
                let failureMessage = "Invalid message at Supervisor!"
                failwith failureMessage
            return! loop()
        }
        loop()

    let CreateUsers(numberOfUsers: int) = 
        supervisor <- spawn system "Supervisor" Supervisor
        
        let input: InitInfo = { NumberOfUsers = numberOfUsers; }
        supervisor <! input

        Console.ReadLine() |> ignore
        
        //let response = Async.RunSynchronously(supervisor <? Messages.REGISTER_USERS)
        //printfn "%A" response
