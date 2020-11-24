namespace Models

open System

module Models = 
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
        TweetHead: Tweet;
        FollowingTo: Set<int>;
        HashtagsFollowed: Set<string>;
    }

    type RegisterUserRequest = {
        Handle: string;
        FirstName: string;
        LastName: string;
    }

    type FollowUserRequest = {
        FollowerId: int;
        FolloweeId: int;
    }

    type UnfollowUserRequest = {
        FollowerId: int;
        FolloweeId: int;
    }

    type PostTweetRequest = {
        UserId: int;
        TweetContent: string;
    }

    type RetweetRequest = {
        UserId: int;
        TweetId: int;
    }

    type GetFeedRequest = {
        UserId: int
    }

    type GetMentionTweetsRequest = {
        UserId: int;
    }

    type GetHashtagTweetsRequest = {
        Hashtags: Set<string>;
    }

    type GetFollowersTweetsRequest = {
        UserId: int;
    }

    type ServerRequestType = 
        | RegisterUser of RegisterUserRequest
        | FollowUser of FollowUserRequest
        | UnfollowUser of UnfollowUserRequest
        | PostTweet of PostTweetRequest
        | Retweet of RetweetRequest
        | GetFeed of GetFeedRequest
        | GetMentionTweets of GetMentionTweetsRequest
        | GetHashtagTweets of GetHashtagTweetsRequest
        | GetFollowersTweets of GetFollowersTweetsRequest

