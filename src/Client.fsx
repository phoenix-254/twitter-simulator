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

type PostTweet = { Id: int; Content: string; }
type PostTweetSuccess = { Id: int; }


// 60 hashtags
let hashtags = seq {
    yield "#love";
    yield "#followback";
    yield "#Twitterers"; 
    yield "#tweegram"; 
    yield "#photooftheday";
    yield "#20likes";
    yield "#amazing";
    yield "#smile";
    yield "#follow4follow";
    yield "#like4like";
    yield "#look";
    yield "#instalike";
    yield "#igers";
    yield "#picoftheday";
    yield "#food";
    yield "#instadaily";
    yield "#instafollow";
    yield "#followme";
    yield "#lmao";
    yield "#instagood";
    yield "#bestoftheday";
    yield "#instacool";
    yield "#follow";
    yield "#colorful";
    yield "#style";
    yield "#swag";
    yield "#thanksgiving";
    yield "#thanks" 
    yield "#giving";
    yield "#socialenvy";
    yield "#turkey";
    yield "#turkeyday";
    yield "#food";
    yield "#foodporn";
    yield "#holiday";
    yield "#family";
    yield "#friends";
    yield "#love";
    yield "#instagood";
    yield "#photooftheday";
    yield "#happythanksgiving";
    yield "#celebrate";
    yield "#stuffing";
    yield "#feast";
    yield "#thankful";
    yield "#blessed";
    yield "#fun";
    yield "#happyholidays";
    yield "#holidays";
    yield "#holiday";
    yield "#vacation";
    yield "#winter2016";
    yield "#2016";
    yield "#2017";
    yield "#happyholidays2016";
    yield "#presents";
    yield "#parties";
    yield "#fun";
    yield "#happy";
    yield "#family";
    yield "#love";
}
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
let celebrityFollowersRange = [|50.0; 80.0;|]

let mutable influencerCount: int = 0
let influencerPercent: float = 11.5
let influencerFollowersRange = [|8.0; 35.0;|]

let mutable commonMenCount: int = 0
let commonMenFollowersRange = [|0.0; 1.0;|]

// Followers distribution map
// [Min, Max] of UserId -> [Min, Max] of followers count
let followersDistribution = new Dictionary<int[], int[]>()

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

let calculateFollowersDistribution() = 
    let mutable prevEnd = 1

    followersDistribution.Add(([|prevEnd; prevEnd + celebrityCount;|], [|percentOf(totalUsers, celebrityFollowersRange.[0]); percentOf(totalUsers, celebrityFollowersRange.[1]);|]))
    prevEnd <- prevEnd + celebrityCount

    followersDistribution.Add(
        ([|prevEnd; prevEnd + influencerCount;|], [|percentOf(totalUsers, influencerFollowersRange.[0]);
        percentOf(totalUsers, influencerFollowersRange.[1]);|])
    )
    prevEnd <- prevEnd + influencerCount
    
    followersDistribution.Add(
        ([|prevEnd; totalUsers + 1;|], [|percentOf(totalUsers, commonMenFollowersRange.[0]); 
        percentOf(totalUsers, commonMenFollowersRange.[1]);|])
    )
    

type Client() = 
    inherit Actor()
    let mutable userId: int = 0
    let mutable handle: string = ""
    let mutable firstName: string = ""
    let mutable lastName: string = ""

    let mutable followersToCreate = 0
    let mutable followersCreated = 0

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

    let getRandomFollowerPair() = 
        [|random.Next(1, totalUsers + 1); userId;|]

    let generateTweet(userId: int) =
        // use user id range to generate tweet frequency 
        // Users in celebrity range will tweet more frequently than other users
        // use a random function to select a number from 1-10 and then use this number to randomly pick a hashtag from the sequence
        // for the tweet generation, check the users tweet dictionary size and post a tweet "tweet test " + tweetdictionary size++ 
        // Put user mentions by checking the following map and get a random user from that map 
        "test"

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
                |> List.iter(fun i ->   let pair = getRandomFollowerPair()
                                        let request: FollowUserRequest = {
                                            FollowerId = pair.[0];
                                            FolloweeId = pair.[1];
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
        | :? PostTweet as request -> 
            let req: PostTweetRequest = {
                UserId = request.Id;
                Content = request.Content;
            }
            server.Tell req
        | :? PostTweetResponse as response ->
            printfn "%A" response
            if response.Success then
                printfn "Your tweet was posted with TWEET_ID: %d" response.TweetId
            else
                printfn "Your tweet was NOT posted"
        | :? PrintInfo as req -> 
            server.Tell req
        | _ -> ()

type Supervisor() = 
    inherit Actor()
    let mutable numberOfUsersCreated: int = 0
    let mutable parent: IActorRef = null
    
    let mutable followerDone = 0
    let mutable tweetsPosted = 0

    override x.OnReceive (message: obj) = 
        match message with
        | :? Init as init -> 
            totalUsers <- init.TotalUsers
            parent <- x.Sender

            calculateUserCategoryCount()
            calculateFollowersDistribution()

            [1 .. totalUsers]
            |> List.iter (fun id -> let client = system.ActorOf(Props(typedefof<Client>), ("Client" + (id |> string)))
                                    clients.Add(client)
                                    let request: RegisterUser = { Id = id; }
                                    client.Tell request)
            |> ignore
        | :? RegisterUserSuccess as response -> 
            numberOfUsersCreated <- numberOfUsersCreated + 1
            if  numberOfUsersCreated = totalUsers then
                printfn "user creation done!"
                let request: CreateFollowers = { FakeProp = 0; }
                x.Self.Tell request
        | :? CreateFollowers as fake -> 
            printfn "creating followers."
            [1 .. totalUsers]
            |> List.iter (fun id -> let req: CreateFollowers = { FakeProp = 0; }
                                    clients.[id-1].Tell req)
            |> ignore
        | :? CreateFollowersSuccess as fake -> 
            followerDone <- followerDone + 1
            if followerDone = totalUsers then
                printfn "follower generation done!"
                // [1 .. totalUsers]
                // |> List.iter (fun id -> let req: PrintInfo = { Id = id; }
                //                         clients.[id-1].Tell req)
                // |> ignore

                let r1: PostTweet = { Id = 2; Content = "good morning #morning #fresh @User4 @User856 yay" }
                clients.[1].Tell r1

                let r2: PostTweet = { Id = 2; Content = "good morning #sunrise @User10 @User856 yay" }
                clients.[1].Tell r2

                let r3: PostTweet = { Id = 298; Content = "awesome click #sunrise#morning @User2 " }
                clients.[297].Tell r3
        | :? PostTweetSuccess as response ->
            // Logic for posting Tweets
            tweetsPosted <- tweetsPosted + 1
        | _ -> ()

let CreateUsers(numberOfUsers: int) = 
    supervisor <- system.ActorOf(Props(typedefof<Supervisor>), "Supervisor")

    let (task:Async<Shutdown>) = (supervisor <? { TotalUsers = numberOfUsers; })
    let response = Async.RunSynchronously (task)
    printfn "%A" response
    supervisor.Tell(PoisonPill.Instance)

let args = Environment.GetCommandLineArgs()
CreateUsers(args.[3] |> int)
