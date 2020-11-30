#load @"./Client.fsx"

open Client

let main (numberOfUsers: int) = 
    Client.CreateUsers(numberOfUsers)

match fsi.CommandLineArgs with
    | [|_; n|] -> 
        main ((n |> int))
    | _ -> printfn "Error: Invalid Arguments."