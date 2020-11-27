namespace TwitterClient

#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#load @"./Models.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Configuration

open System
open System.Collections.Generic

open Models

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

    // Global server node reference
    let server = system.ActorSelection("akka.tcp://TwitterEngine@192.168.0.193:7777/user/Server")

    // Client nodes list
    let clients = new List<IActorRef>()

    // Returns number of followee based on the id of the user
    let getFolloweeCount (id: int) = 
        if id < 100 then 8
        else 25

    let Client (mailbox: Actor<_>) = 
        let mutable userId: int = 0
        let mutable handle: string = ""
        let mutable firstName: string = ""
        let mutable lastName: string = ""

        let random = Random()

        // Returns the number of followers this user should have based on the Zipf distribution
        let getFollowersCount () = 
            if userId < 100 then 10
            else 50

        let getRandomFollwer () = 
            let follower: Models.FollowUserRequest = {
                FollowerId = random.Next(clients.Count);
                FolloweeId = userId;
            }
            follower

        let rec loop () = actor {
            let! message = mailbox.Receive()

            match message with
                | Models.MessageType.RegisterUserRequest request ->
                    printfn "sending register user request to server: %A" request
                    server <! Models.MessageType.RegisterUserRequest request
                | Models.MessageType.RegisterUserResponse response -> 
                    if response.Success then
                        userId <- response.Id
                        handle <- response.Handle
                        firstName <- response.FirstName
                        lastName <- response.LastName

                        mailbox.Sender() <! Models.MessageType.GenerateUserSuccess
                    else
                        printfn "User registeration failed!"
                | Models.MessageType.GenerateFollowers ->
                    let followersCount = getFollowersCount()
                    [1 .. followersCount]
                    |> List.iter(fun i ->   server <! Models.MessageType.FollowUserRequest (getRandomFollwer()))
                    |> ignore
                | _ -> 
                    server <! "Hello Server!"
                    printfn "Received message - %A from Simulator." message

            return! loop()
        }
        loop ()

    let Supervisor (mailbox: Actor<_>) = 
        let mutable numberOfUsers: int = 0
        let mutable numberOfUsersCreated: int = 0

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
                | Models.MessageType.InitClient input ->
                    numberOfUsers <- input.NumberOfUsers
                | Models.MessageType.GenerateUsers ->
                    [1 .. numberOfUsers]
                    |> List.iter(fun i ->   let client = spawn system ("Client" + (i |> string)) Client
                                            clients.Add(client)
                                            let request: Models.RegisterUserRequest = {
                                                Handle = ("User_" + (i |> string));
                                                FirstName = generateRandomName();
                                                LastName = generateRandomName();
                                            }
                                            client <! Models.MessageType.RegisterUserRequest request)
                    |> ignore
                | Models.MessageType.GenerateUserSuccess ->
                    numberOfUsersCreated <- numberOfUsersCreated + 1
                    if numberOfUsersCreated = numberOfUsers then
                        [1 .. numberOfUsers]
                        |> List.iter(fun i ->   clients.[i] <! Models.MessageType.GenerateFollowers)
                        |> ignore
                | _ ->
                    printfn "Invalid message %A at supervisor!" message 
            return! loop()
        }

        loop()

    
    let supervisor = spawn system "Supervisor" Supervisor

    let registerUsers (numberOfUsers: int) = 
        let input: Models.InitClient = {
            NumberOfUsers = numberOfUsers;
        }
        supervisor <! Models.MessageType.InitClient input
        supervisor <! Models.MessageType.GenerateUsers