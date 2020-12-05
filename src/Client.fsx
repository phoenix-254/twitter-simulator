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
type FinishTweeting = { Id: int; RetweetCount: int }

type UpdateUserStatus = { IsOnline: bool; }
type SimulateUserStatusUpdate = { FakeSimulateStatusProp: int; }

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
                port = 5556
                hostname = localhost
            }
        }")

let system = ActorSystem.Create("Twitter", configuration)

let server = system.ActorSelection("akka.tcp://Twitter@localhost:5555/user/Server")

// Total number of registered users
let mutable totalUsers = 0

// User categories
let mutable celebrityCount: int = 0
let celebrityPercent: float = 0.05
let celebrityFollowersRange = [|15.0; 30.0;|]
let celebrityTweetCountRange = [|200; 250;|]
let celebrityIdRange = new List<int>()
let mutable celebrityFollowersCount = 0
let mutable celebrityTweetsCount = 0

let mutable influencerCount: int = 0
let influencerPercent: float = 0.5
let influencerFollowersRange = [|3.0; 8.0;|]
let influencerTweetCountRange = [|100; 150;|]
let influencerIdRange = new List<int>()
let mutable influencerFollowersCount = 0
let mutable influencerTweetsCount = 0

let mutable commonMenCount: int = 0
let commonMenFollowersRange = [|0.0; 0.5;|]
let commonMenTweetCountRange = [|15; 30;|]
let mutable commonMenFollowersCount = 0
let mutable commonMenTweetsCount = 0

let mutable totalNumberOfFollowers = 0

// Followers distribution map
// [Min, Max] of UserId -> [Min, Max] of followers count
let followersDistribution = new Dictionary<int[], int[]>()

// Tweet Count distribution map
// [Min, Max] of UserId -> [Min, Max] of tweet count
let tweetCountDistribution = new Dictionary<int[], int[]>()

let hashtagFrequency = 5
let mentionFrequency = 10
let retweetFrequency = 25

// Global supervisor actor reference
let mutable supervisor: IActorRef = null

// Client nodes list
let clients = new List<IActorRef>()

let percentOf(number: int, percent: float) = 
    ((number |> float) * percent / 100.0) |> int

let calculateUserCategoryCount() = 
    celebrityCount <- Math.Max(1, percentOf(totalUsers, celebrityPercent))
    influencerCount <- Math.Max(3, percentOf(totalUsers, influencerPercent))
    commonMenCount <- totalUsers - (celebrityCount + influencerCount)

let calculateDistributions() = 
    let mutable prevEnd = 1

    celebrityIdRange.Add(prevEnd)
    celebrityIdRange.Add(prevEnd + celebrityCount)
    followersDistribution.Add(([|prevEnd; prevEnd + celebrityCount;|], [|percentOf(totalUsers, celebrityFollowersRange.[0]); percentOf(totalUsers, celebrityFollowersRange.[1]);|]))
    tweetCountDistribution.Add(([|prevEnd; prevEnd + celebrityCount;|], celebrityTweetCountRange))
    prevEnd <- prevEnd + celebrityCount

    influencerIdRange.Add(prevEnd)
    influencerIdRange.Add(prevEnd + influencerCount)
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

    let mutable retweetCount = 0

    let mutable isOnline = true

    let random = Random()

    let myFeed = new LinkedList<TweetData>()
    let maxTweetPerFeed = 100

    let mutable feedReceived = 0

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
        totalNumberOfFollowers <- totalNumberOfFollowers + 1

        if userId >= celebrityIdRange.[0] && userId < celebrityIdRange.[1] then
            celebrityFollowersCount <- celebrityFollowersCount + count
        else if userId >= influencerIdRange.[0] && userId < influencerIdRange.[1] then
            influencerFollowersCount <- influencerFollowersCount + count
        else
            commonMenFollowersCount <- commonMenFollowersCount + count

        count

    let generateRandomTweet(): string = 
        let mutable tweet: string = "Hey there! User" + (userId |> string) + " here. Making my tweet number " + ((tweetsPosted + 1) |> string)
        if (tweetsPosted % hashtagFrequency) = 0 then
            tweet <- tweet + " " + hashtags.[random.Next(hashtags.Length)]
        if (tweetsPosted % mentionFrequency) = 0 then
            tweet <- tweet + " @User" + (random.Next(totalUsers) |> string)
        tweet

    override x.OnReceive (message: obj) =
        match message with
        | :? RegisterUser as user -> 
            let request: RegisterUserRequest = {
                Handle = ("User" + (user.Id |> string));
                FirstName = generateRandomName();
                LastName = generateRandomName();
                ActorRef = x.Self;
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
            if tweetsPosted < tweetsToPost then  
                let tweetReq: PostTweet = { Content = generateRandomTweet(); }
                x.Self.Tell tweetReq
        | :? PostTweet as request -> 
            let req: PostTweetRequest = {
                UserId = userId;
                Content = request.Content;
            }
            server.Tell req
        | :? PostTweetResponse as response ->
            tweetsPosted <- tweetsPosted + 1
            if tweetsPosted = tweetsToPost then
                let req: FinishTweeting = { Id = userId; RetweetCount = retweetCount; }
                supervisor.Tell req
            else 
                if isOnline then
                    let tweetReq: PostTweet = { Content = generateRandomTweet(); }
                    x.Self.Tell tweetReq
        | :? UpdateFeedResponse as response -> 
            myFeed.AddFirst(response.Tweet) |> ignore
            if myFeed.Count > maxTweetPerFeed then myFeed.RemoveLast() |> ignore
            feedReceived <- feedReceived + 1
            if feedReceived % retweetFrequency = 0 && response.Tweet.PostedById <> userId && isOnline then
                retweetCount <- retweetCount + 1
                let retweetReq: RetweetRequest = {
                    UserId = userId;
                    TweetId = response.Tweet.Id;
                    OriginalUserId = response.Tweet.PostedById;
                }
                server.Tell retweetReq
        | :? RetweetResponse as response -> ()
        | :? UpdateUserStatus as request -> 
            let req: UpdateUserStatusRequest = {
                UserId = userId;
                IsOnline = request.IsOnline;
            }
            server.Tell req
        | :? PrintInfo as req -> 
            server.Tell req
        | _ -> ()

type Supervisor() = 
    inherit Actor()
    let mutable numberOfUsersCreated: int = 0
    let mutable parent: IActorRef = null

    let mutable offlineUsersCount = 0
    let offlineUsersPercent = [|10; 20|]
    
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
                count <- random.Next(entry.Value.[0], entry.Value.[1] + 1)
        totalNumberOfTweets <- totalNumberOfTweets + count

        if userId >= celebrityIdRange.[0] && userId < celebrityIdRange.[1] then
            celebrityTweetsCount <- celebrityTweetsCount + count
        else if userId >= influencerIdRange.[0] && userId < influencerIdRange.[1] then
            influencerTweetsCount <- influencerTweetsCount + count
        else
            commonMenTweetsCount <- commonMenTweetsCount + count

        count

    override x.OnReceive (message: obj) = 
        match message with
        | :? Init as init -> 
            totalUsers <- init.TotalUsers
            parent <- x.Sender

            offlineUsersCount <- percentOf(totalUsers, float(random.Next(offlineUsersPercent.[0], offlineUsersPercent.[1])))

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
        | :? SimulateUserStatusUpdate as fake -> 
            [1 .. offlineUsersCount]
            |> List.iter(fun i ->   let randomId = random.Next(totalUsers)
                                    clients.[randomId].Tell { IsOnline = false; })
        | :? CreateFollowersSuccess as fake -> 
            userCountWithFollowersCreated <- userCountWithFollowersCreated + 1
            if userCountWithFollowersCreated = totalUsers then
                x.Self.Tell { FakeSimulateStatusProp = 0; }
                printfn "-------------------------------------------------\n"
                printfn "Total number of users: %d" totalUsers
                printfn "Total number of users offline: %d" offlineUsersCount
                printfn "Total number of celebrities: %d" celebrityCount
                printfn "Total number of influencers: %d" influencerCount
                printfn "Total number of common men: %d" commonMenCount

                printfn "\n-------------------------------------------------\n"
                printfn "------------Follower Distribution------------"
                printfn "Total follower for celebrities: %d" celebrityFollowersCount
                printfn "Average follower for celebrities: %d" (int(celebrityFollowersCount / celebrityCount))
                printfn "Total follower for influencers: %d" influencerFollowersCount
                printfn "Average follower for influencers: %d" (int(influencerFollowersCount / influencerCount))
                printfn "Total follower for common men: %d" commonMenFollowersCount
                printfn "Average follower for common men: %d" (int(commonMenFollowersCount / commonMenCount))
                
                startTime <- DateTime.Now
                [1 .. totalUsers]
                |> List.iter (fun id -> let req: InitiateTweeting = { NumberOfTweets = getTweetCountForUser(id); }
                                        clients.[id-1].Tell req)
                |> ignore
        | :? FinishTweeting as response -> 
            totalNumberOfTweets <- totalNumberOfTweets + response.RetweetCount
            if response.Id >= celebrityIdRange.[0] && response.Id < celebrityIdRange.[1] then
                celebrityTweetsCount <- celebrityTweetsCount + response.RetweetCount
            else if response.Id >= influencerIdRange.[0] && response.Id < influencerIdRange.[1] then
                influencerTweetsCount <- influencerTweetsCount + response.RetweetCount
            else
                commonMenTweetsCount <- commonMenTweetsCount + response.RetweetCount

            userCountWithTweetsPosted <- userCountWithTweetsPosted + 1
            if userCountWithTweetsPosted = totalUsers then
                endTime <- DateTime.Now
                
                let timeTaken = (endTime - startTime).TotalSeconds
                
                printfn "\n-------------------------------------------------\n"
                printfn "------------Tweet Distribution------------"
                printfn "Total number of tweets by celebrities: %d" celebrityTweetsCount
                printfn "Average number of tweets by celebrities: %d" (int(celebrityTweetsCount / celebrityCount))
                printfn "Total number of tweets by influencers: %d" influencerTweetsCount
                printfn "Average number of tweets by influencers: %d" (int(influencerTweetsCount / influencerCount))
                printfn "Total number of tweets by common men: %d" commonMenTweetsCount
                printfn "Average number of tweets by common men: %d" (int(commonMenTweetsCount / commonMenCount))

                printfn "\n-------------------------------------------------\n"
                printfn "Total tweets made: %d" totalNumberOfTweets
                printfn "Total time taken: %d seconds" (int(Math.Floor(timeTaken)))
                printfn "Number of tweets per second: %d\n" (int(Math.Ceiling((float(totalNumberOfTweets) / timeTaken))))
                
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
