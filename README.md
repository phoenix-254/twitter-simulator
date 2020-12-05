# Twitter-Simulator

---

## Shaishav Shah - 1136-3317
## Akib Maredia - 3885-6489

---

### Files overview

* MessageType.fsx - Contains request response type definitions used for communication between a Client and a Server.
* Server.fsx - Contains all the logic related to User processing, Tweet processing etc. Runs as a standalone process.
* Client.fsx - Contains simulation logic and Client actor logic where each Client corresponds to a single user and performs all the functions for a user. 

### Instructions to run the project

* Unzip the file `AkibMaredia_ShaishavShah_Proj4.1.zip`
* Move to `src` directory
* Start the Twitter Engine using the command `dotnet fsi --langversion:preview Server.fsx` and wait for the message "Server is running!" to allow the Server to boot up
* In a new terminal instance or in a new terminal tab, start the Simulator process by passing the number of users as an argument -
eg: `dotnet fsi --langversion:preview Client.fsx 1000` 
* The Simulator will run until all the clients finishes sending the tweets to the engine, and would print out the relevant stats to the screen.

### Functionalities implemented

* The user can register himself to Twitter
* The user can login to or logout from the Twitter (live connection / disconnection)
* The user can follow / unfollow another user already registered
* The followers are distributed using the Zipf distribution
* The user can send tweets with / without hashtags and / or mentions (mention another user)
* The user can retweet the tweets posted by the users he's following to let his followers know about some interesting tweet
* The user can see his feed i.e the tweets posted by him, tweets posted by the users he's following, and the tweets where he is mentioned
* We divided the total number of users in 3 categories: Celebrities, Influencers, and Common Men
  * Celebrities: Very small percent of users having very high number of followers and posting most number of tweets
  * Influencers: Small percent (little higher than the celebrities) of users having high number (lesser than celebrities) of followers and posting second most number of tweets  
  * Common Men: All the remaining group of users having very less follower count and posting very less tweets


### Performance

#### For 1000 Users

|             | #    | #Followers | Avg. #Followers | #Tweets + #Retweets | Avg. #Tweets |
|       :-:   | :-:  | :-:        | :-:             | :-:                 | :-:          |
| Total       | 1000 | 2577       | -               | 24785               | -            |
| Celebrities | 1    | 277        | 277             | 244                 | 244          |
| Influencers | 5    | 298        | 59              | 653                 | 130          |
| Common Men  | 994  | 2002       | 2               | 23888               | 24           |

Offline users :- 17.0% = 170 (Chosen randomly between 10% - 20% of total users)

Total time taken :- 21 Seconds

Number of tweets handles per second :- 1128


#### For 5000 Users

|             | #    | #Followers | Avg. #Followers | #Tweets + #Retweets | Avg. #Tweets |
|       :-:   | :-:  | :-:        | :-:             | :-:                 | :-:          |
| Total       | 5000 | 67894      | -               | 194899              | -            |
| Celebrities | 2    | 2090       | 1045            | 571                 | 285          |
| Influencers | 25   | 6458       | 258             | 3807                | 152          |
| Common Men  | 4973 | 59346      | 11              | 190521              | 38           |

Offline users :- 14.0% = 700 (Chosen randomly between 10% - 20% of total users)

Total time taken :- 610 Seconds

Number of tweets handles per second :- 320


#### For 10000 Users

|             | #     | #Followers | Avg. #Followers | #Tweets + #Retweets  | Avg. #Tweets |
|       :-:   | :-:   | :-:        | :-:             | :-:                  | :-:          |
| Total       | 10000 | 280825     | -               | 447821               | -            |
| Celebrities | 5     | 10312      | 2062            | 1539                 | 308          |
| Influencers | 50    | 28692      | 573             | 8205                 | 165          |
| Common Men  | 9945  | 241821     | 24              | 438077               | 45           |

Offline users :- 15.0% = 1500 (Chosen randomly between 10% - 20% of total users)

Total time taken :- 2073 Seconds

Number of tweets handles per second :- 217
