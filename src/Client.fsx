#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

#load @"./MessageTypes.fsx"

open Akka.Actor
open Akka.FSharp
open Akka.Remote
open Akka.Configuration

open System
open System.Collections.Generic

open MessageTypes

type Init = { TotalUsers: int; }
type Shutdown = { Message: string; }

type RegisterUser = { Id: int; }
type RegisterUserSuccess = { Id: int; }

type CreateFollowers = { FakeProp: int; }
type CreateFollowersSuccess = { FakeProp: int; }

type InitiateTweeting = { NumberOfTweets: int; }
type PostTweet = { Content: string; }
type FinishTweeting = { Id: int; }

let hashtags = [
    "#love"; "#followback"; "#Twitterers"; "#tweegram"; "#photooftheday";
    "#20likes"; "#amazing"; "#smile"; "#follow4follow"; "#like4like"; "#look";
    "#instalike"; "#igers"; "#picoftheday"; "#foodgiving"; "#instadaily"; "#instafollow";
    "#followme"; "#lmao"; "#instagood"; "#bestoftheday"; "#instacool"; "#follow";
    "#colorful"; "#style"; "#swag"; "#thanksgiving"; "#thanks"; "#giving";
    "#socialenvy"; "#turkey"; "#turkeyday"; "#food"; "#dinner"; "#holiday"; "#family";
    "#friends"; "#home"; "#tweeter"; "#tweetoftheday"; "#happythanksgiving";
    "#celebrate"; "#stuffing"; "#feast"; "#thankful"; "#blessed"; "#fungathering";
    "#happyholidays"; "#holidayseason"; "#familyholiday"; "#vacation"; "#winter2020"; 
    "#2020"; "#2021"; "#happyholidays2020"; "#presents"; "#parties"; "#fun"; 
    "#gifts"; "#familylove"; "#happiness";
]

// Remote Configuration
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                port = 6666
                hostname = localhost
            }
        }")

let system = ActorSystem.Create("Twitter", configuration)

let server = system.ActorSelection("akka.tcp://Twitter@localhost:5555/user/Server")

// Total number of registered users
let mutable totalUsers = 0

// User categories
let mutable celebrityCount: int = 0
let celebrityPercent: float = 0.5
let celebrityFollowersRange = [|35.0; 80.0;|]
let celebrityTweetCountRange = [|50; 80;|]

let mutable influencerCount: int = 0
let influencerPercent: float = 11.5
let influencerFollowersRange = [|5.0; 30.0;|]
let influencerTweetCountRange = [|30; 50;|]

let mutable commonMenCount: int = 0
let commonMenFollowersRange = [|0.0; 1.0;|]
let commonMenTweetCountRange = [|5; 10;|]

// Followers distribution map
// [Min, Max] of UserId -> [Min, Max] of followers count
let followersDistribution = new Dictionary<int[], int[]>()

// Tweet Count distribution map
// [Min, Max] of UserId -> [Min, Max] of tweet count
let tweetCountDistribution = new Dictionary<int[], int[]>()

let hashtagFrequency = 5
let mentionFrequency = 10

// Global supervisor actor reference
let mutable supervisor: IActorRef = null

// Client nodes list
let clients = new List<IActorRef>()

let percentOf(number: int, percent: float) = 
    ((number |> float) * percent / 100.0) |> int

let calculateUserCategoryCount() = 
    celebrityCount <- percentOf(totalUsers, celebrityPercent)
    influencerCount <- percentOf(totalUsers, influencerPercent)
    commonMenCount <- totalUsers - (celebrityCount + influencerCount)

let calculateDistributions() = 
    let mutable prevEnd = 1

    followersDistribution.Add(([|prevEnd; prevEnd + celebrityCount;|], [|percentOf(totalUsers, celebrityFollowersRange.[0]); percentOf(totalUsers, celebrityFollowersRange.[1]);|]))
    tweetCountDistribution.Add(([|prevEnd; prevEnd + celebrityCount;|], celebrityTweetCountRange))
    prevEnd <- prevEnd + celebrityCount

    followersDistribution.Add(([|prevEnd; prevEnd + influencerCount;|], [|percentOf(totalUsers, influencerFollowersRange.[0]); percentOf(totalUsers, influencerFollowersRange.[1]);|]))
    tweetCountDistribution.Add(([|prevEnd; prevEnd + influencerCount;|], influencerTweetCountRange))
    prevEnd <- prevEnd + influencerCount
    
    followersDistribution.Add(([|prevEnd; totalUsers + 1;|], [|percentOf(totalUsers, commonMenFollowersRange.[0]); percentOf(totalUsers, commonMenFollowersRange.[1]);|]))    
    tweetCountDistribution.Add(([|prevEnd; totalUsers + 1;|], commonMenTweetCountRange))

type Client() = 
    inherit Actor()
    let mutable userId: int = 0
    let mutable handle: string = ""
    let mutable firstName: string = ""
    let mutable lastName: string = ""

    let mutable followersToCreate = 0
    let mutable followersCreated = 0

    let mutable tweetsToPost = 0
    let mutable tweetsPosted = 0

    let random = Random()

    let upperCase = [|'A' .. 'Z'|]
    let lowerCase = [|'a' .. 'z'|]
    let generateRandomName(): string = 
        let len = random.Next(5)
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

    let generateRandomTweet(): string = 
        let mutable tweet: string = "Hey there! User" + (userId |> string) + " here. Making my tweet number " + ((tweetsPosted + 1) |> string)
        if (tweetsPosted % hashtagFrequency) = 0 then
            tweet <- tweet + " " + hashtags.[random.Next(hashtags.Length)]
        if (tweetsPosted % mentionFrequency) = 0 then
            tweet <- tweet + " @User" + (random.Next(clients.Count) |> string)
        tweet

    override x.OnReceive (message: obj) =
        match message with
        | :? RegisterUser as user -> 
            let request: RegisterUserRequest = {
                Handle = ("User" + (user.Id |> string));
                FirstName = generateRandomName();
                LastName = generateRandomName();
            }
            server.Tell request
        | :? RegisterUserResponse as response -> 
            if response.Success then 
                userId <- response.Id
                handle <- response.Handle
                firstName <- response.FirstName
                lastName <- response.LastName

                let res: RegisterUserSuccess = { Id = response.Id; }
                supervisor.Tell res
            else ()
        | :? CreateFollowers as fake -> 
            let numberOfFollowers = getNumberOfFollowers()
            if numberOfFollowers = 0 then
                let req: CreateFollowersSuccess = { FakeProp = 0; }
                supervisor.Tell req
            else
                followersToCreate <- numberOfFollowers
                [1 .. numberOfFollowers]
                |> List.iter(fun i ->   let request: FollowUserRequest = {
                                            FollowerId = random.Next(1, totalUsers + 1);
                                            FolloweeId = userId;
                                        }
                                        server.Tell request)
                |> ignore
        | :? FollowUserResponse as response -> 
            followersCreated <- followersCreated + 1
            if followersCreated = followersToCreate then 
                let req: CreateFollowersSuccess = { FakeProp = 0; }
                supervisor.Tell req
            if response.Success then ()
            else ()
        | :? InitiateTweeting as request -> 
            tweetsToPost <- request.NumberOfTweets
            let self = x.Self
            [1 .. request.NumberOfTweets]
            |> List.iter(fun i ->   let tweetReq: PostTweet = { 
                                        Content = generateRandomTweet(); 
                                    }
                                    self.Tell tweetReq)
            |> ignore
        | :? PostTweet as request -> 
            let req: PostTweetRequest = {
                UserId = userId;
                Content = request.Content;
            }
            server.Tell req
        | :? PostTweetResponse as response ->
            tweetsPosted <- tweetsPosted + 1
            if tweetsPosted = tweetsToPost then
                let req: FinishTweeting = { Id = userId; }
                supervisor.Tell req
        | :? GetFeedResponse as response -> 
            printfn "%A" response
        | :? PrintInfo as req -> 
            server.Tell req
        | _ -> ()

type Supervisor() = 
    inherit Actor()
    let mutable numberOfUsersCreated: int = 0
    let mutable parent: IActorRef = null
    
    let mutable userCountWithFollowersCreated = 0
    let mutable userCountWithTweetsPosted = 0

    let mutable startTime = DateTime.Now
    let mutable endTime = DateTime.Now

    let mutable totalNumberOfTweets = 0

    let random = Random()

    let getTweetCountForUser (userId: int) = 
        let mutable count = 0
        for entry in tweetCountDistribution do
            if (userId >= entry.Key.[0] && userId < entry.Key.[1]) then 
                count <- random.Next(entry.Value.[0], entry.Value.[1])
        totalNumberOfTweets <- totalNumberOfTweets + count
        count

    override x.OnReceive (message: obj) = 
        match message with
        | :? Init as init -> 
            totalUsers <- init.TotalUsers
            parent <- x.Sender

            calculateUserCategoryCount()
            calculateDistributions()

            [1 .. totalUsers]
            |> List.iter (fun id -> let client = system.ActorOf(Props(typedefof<Client>), ("Client" + (id |> string)))
                                    clients.Add(client)
                                    let request: RegisterUser = { Id = id; }
                                    client.Tell request)
            |> ignore
        | :? RegisterUserSuccess as response -> 
            numberOfUsersCreated <- numberOfUsersCreated + 1
            if  numberOfUsersCreated = totalUsers then
                let request: CreateFollowers = { FakeProp = 0; }
                x.Self.Tell request
        | :? CreateFollowers as fake -> 
            [1 .. totalUsers]
            |> List.iter (fun id -> let req: CreateFollowers = { FakeProp = 0; }
                                    clients.[id-1].Tell req)
            |> ignore
        | :? CreateFollowersSuccess as fake -> 
            userCountWithFollowersCreated <- userCountWithFollowersCreated + 1
            if userCountWithFollowersCreated = totalUsers then
                startTime <- DateTime.Now
                [1 .. totalUsers]
                |> List.iter (fun id -> let req: InitiateTweeting = { NumberOfTweets = getTweetCountForUser(id); }
                                        clients.[id-1].Tell req)
                |> ignore
        | :? FinishTweeting as response -> 
            userCountWithTweetsPosted <- userCountWithTweetsPosted + 1
            if userCountWithTweetsPosted = totalUsers then
                endTime <- DateTime.Now
                let timeTaken = (endTime - startTime).TotalSeconds
                printfn "Total tweets made: %d" totalNumberOfTweets
                printfn "Total time taken: %A seconds" timeTaken
                printfn "Number of requests per second: %A" (float(totalNumberOfTweets) / timeTaken)
                let shutdown: Shutdown = { Message = "Done!"; }
                parent.Tell shutdown
        | _ -> ()

let StartSimulation(numberOfUsers: int) = 
    supervisor <- system.ActorOf(Props(typedefof<Supervisor>), "Supervisor")
    let (task:Async<Shutdown>) = (supervisor <? { TotalUsers = numberOfUsers; })
    Async.RunSynchronously (task) |> ignore
    supervisor.Tell(PoisonPill.Instance)

let args = Environment.GetCommandLineArgs()
StartSimulation(args.[3] |> int)
