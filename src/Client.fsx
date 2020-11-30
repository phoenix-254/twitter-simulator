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

    let mutable totalUsers = 0
        
    // Global supervisor actor reference
    let mutable supervisor: IActorRef = null
    
    // Client nodes list
    let clients = new List<IActorRef>()

    type InitInfo = { NumberOfUsers: int; }
    type RegisterUser = { Id: int; }
    
    let Client (mailbox: Actor<_>) =
        let mutable userId: int = 0
        let mutable handle: string = ""
        let mutable firstName: string = ""
        let mutable lastName: string = ""

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

        let rec loop()= actor{
            let! message = mailbox.Receive();
    
            match box message with 
                | :? RegisterUser as input -> 
                    let request = 
                        Messages.REGISTER_USER_REQUEST + "|" + 
                        ("User" + (input.Id |> string)) + "|" +
                        (generateRandomName()) + "|" +
                        (generateRandomName())
                    server <! request
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
                    | _ -> printfn "Invalid message %s at client %d." response userId
                | _ -> 
                    let failureMessage = "Invalid message at Client!"
                    failwith failureMessage
            return! loop()
        }            
        loop()
    
    let Supervisor (mailbox: Actor<_>) =
        let mutable numberOfUsersCreated: int = 0
    
        let mutable parent: IActorRef = null
    
        let rec loop()= actor{
            let! message = mailbox.Receive();
            
            match box message with 
            | :? InitInfo as input -> 
                totalUsers <- input.NumberOfUsers
                parent <- mailbox.Sender()
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
                        parent <! "Done"
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
        
        let response = Async.RunSynchronously(supervisor <? Messages.REGISTER_USERS)
        printfn "%A" response
