#load @"./TwitterClient.fsx"
#load @"./Utils.fsx"

open TwitterClient
open Utils

open System

let main numberOfUsers = 
    TwitterClient.registerUsers numberOfUsers

match fsi.CommandLineArgs with
    | [|_; numberOfUsers;|] ->
        main (Utils.strToInt numberOfUsers)
    | _ -> printfn "Error: Invalid Arguments"
