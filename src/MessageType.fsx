namespace MessageType

open System.Collections.Generic

module MessageType = 
    type Tweet = {
        Id: int;
        Content: string;
        CreatedTime: int;
        NextTweet: Tweet;
    }

    type User = {
        Id: int;
        Handle: string;
        FirstName: string;
        LastName: string;
        TweetHead: Tweet option;
        FollowingTo: HashSet<int>;
        HashtagsFollowed: HashSet<string>;
    }

    type InitClient = {
        NumberOfUsers: int;
    } 

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
        TweetContent: string;
    }

    type PostTweetResponse = {
        Success: bool;
    }

    type RetweetRequest = {
        UserId: int;
        TweetId: int;
    }

    type RetweetResponse = {
        Success: bool;
    }

    type GetFeedRequest = {
        UserId: int
    }

    type GetFeedResponse = {
        Success: bool;
    }

    type GetMentionTweetsRequest = {
        UserId: int;
    }

    type GetMentionTweetsResponse = {
        Success: bool;
    }

    type GetHashtagTweetsRequest = {
        Hashtags: HashSet<string>;
    }

    type GetHashtagTweetsResponse = {
        Success: bool;
    }

    type GetFollowersTweetsRequest = {
        UserId: int;
    }

    type GetFollowersTweetsResponse = {
        Success: bool;
    }

    type MessageType = 
        | PrintInfo

        | InitClient of InitClient

        | GenerateUsers
        | GenerateUserSuccess
        
        | GenerateFollowers
        | GenerateFollowersSuccess

        | RegisterUserRequest of RegisterUserRequest
        | RegisterUserResponse of RegisterUserResponse

        | FollowUserRequest of FollowUserRequest
        | FollowUserResponse of FollowUserResponse
        
        | UnfollowUserRequest of UnfollowUserRequest
        | UnfollowUserResponse of UnfollowUserResponse
        
        | PostTweetRequest of PostTweetRequest
        | PostTweetResponse of PostTweetResponse
        
        | RetweetRequest of RetweetRequest
        | RetweetResponse of RetweetResponse
        
        | GetFeedRequest of GetFeedRequest
        | GetFeedResponse of GetFeedResponse
        
        | GetMentionTweetsRequest of GetMentionTweetsRequest
        | GetMentionTweetsResponse of GetMentionTweetsResponse
        
        | GetHashtagTweetsRequest of GetHashtagTweetsRequest
        | GetHashtagTweetsResponse of GetHashtagTweetsResponse
        
        | GetFollowersTweetsRequest of GetFollowersTweetsRequest
        | GetFollowersTweetsResponse of GetFollowersTweetsResponse
