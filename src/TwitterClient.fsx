namespace TwitterClient

#r "nuget: Akka.FSharp"
#load @"./TwitterEngine.fsx"

open TwitterEngine

module TwitterClient = 
    let hello() = 
        TwitterEngine.hello()
        printfn "Hello from Twitter Client!"