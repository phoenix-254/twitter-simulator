# Twitter-Simulator

---

## Shaishav Shah
## Akib Maredia

---

### Instructions to run the project

* Unzip twitter-simulator.zip using ``` unzip twitter-simulator.zip ```
* Go to directory ` twitter-simulator/src `  or ``` cd twitter-simulator/src ```
* Start the serve using the command ``` dotnet fsi --langversion:preview Server.fsx ```
* In a new terminal instance or in a new terminal tab, start the client with the number of users
  using for eg: ``` dotnet fsi --langversion:preview Client.fsx  1000 ``` 
* This will start up the twitter-simulator.

### Overview

* The client can write tweets with hashtags and mentions
* Hashtags are randomly generated in order to add to the tweets
* Mentions in a user's tweets are randome users from their following list
* Clients will randomly login and logoff and receive tweets on login
* Clients will randomly pick a tweet from their feed and retweet
* There is a supervisor which will spawn client actors which will be the number of users
* Once actors are spawned, they will be registered making a request to the server through client
* After users are registered, followers and following will be generated using zipf distribution such that there are 3 ranges of users namely celebrity, influencer and common.
* Celebrities will be a small part of the total number of users and will have most followers up to 80% of the total number of users.
* Influencers will be the second most famous group and will have upto 30% users as followers
* Common users will have upto 1% of users as followers
* Once registration and subscriptions are done, clients will start tweeting with with or without mentions and hashtags
* When tweets are being generated, every user has a queue of feed which is requested from the server.
* Users will receive update in feed from their subscribed users as and when new tweets are posted
* We are not using any external database.

### Performance

| Number of users | Number of tweets/sec  |
|       :-:       |         :-:           |
|     10000       |          5269         |
|     25000       |          3742         |

