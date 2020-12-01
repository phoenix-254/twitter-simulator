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

type PrintInfo = {
    Id: int;
}