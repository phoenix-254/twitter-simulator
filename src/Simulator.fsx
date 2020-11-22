#load @"./TwitterClient.fsx"
#load @"./Utils.fsx"

open TwitterClient
open Utils

let main numberOfUsers = 
    printfn "Number of Users: %d" numberOfUsers
    TwitterClient.hello()
    printfn "Hello from Twitter Simulator!"

match fsi.CommandLineArgs with
    | [|_; numberOfUsers;|] ->
        main (Utils.strToInt numberOfUsers)
    | _ -> printfn "Error: Invalid Arguments"
