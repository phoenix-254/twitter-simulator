#r "nuget: Akka.FSharp"

open Akka.Actor
open Akka.FSharp

open System.Collections.Generic

type RegisterUserRequest = {
    Handle: string;
    FirstName: string;
    LastName: string;
    ActorRef: IActorRef;
}

type RegisterUserResponse = {
    Id: int;
    Handle: string;
    FirstName: string;
    LastName: string;
    Success: bool;
}

type FollowUserRequest = {
    FollowerId: int;
    FolloweeId: int;
}

type FollowUserResponse = {
    Success: bool;
}

type UnfollowUserRequest = {
    FollowerId: int;
    FolloweeId: int;
}

type UnfollowUserResponse = {
    Success: bool;
}

type PostTweetRequest = {
    UserId: int;
    Content: string;
}

type PostTweetResponse = {
    UserId: int;
    TweetId: int;
    Content: string;
    Success: bool;
}

type TweetData = { 
    Id: int;
    Content: string;
    PostedById: int;
    PostedBy: string; 
}

type UpdateFeedResponse = {
    Tweet: TweetData;
}

type RetweetRequest = {
    UserId: int;
    TweetId: int;
    OriginalUserId: int;
}

type RetweetResponse = {
    Success: bool;
}

type PrintInfo = {
    Id: int;
}
