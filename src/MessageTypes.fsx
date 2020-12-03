open System.Collections.Generic

type RegisterUserRequest = {
    Handle: string;
    FirstName: string;
    LastName: string;
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

type GetFeedRequest = {
    UserId: int;
    NumberOfTweets: int;
}

type TweetData = { 
    Id: int;
    Content: string;
    PostedBy: string; 
}

type GetFeedResponse = {
    UserId: int;
    Tweets: HashSet<TweetData>;
}

type PrintInfo = {
    Id: int;
}
