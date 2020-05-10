using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiotNet;

namespace LeaderboardBot
{

    public enum LeaderboardSortTypes { SCORE, KILLS, DEATHS, ASSISTS, CS, VISION };
    public enum TimeFrames { ALL, WEEK, MONTH };

    public class PlayerScore
    {
        public PlayerObject player;
        public float score;

        public PlayerScore(PlayerObject player)
        {
            this.player = player;
        }
    }

    public static class GameUtility
    {
        public async static Task AddGamesToDatabase(Database database, PlayerObject player)
        {
            foreach (string summonerID in player.summonerIDs)
            {

                try
                {
                    List<RiotNet.Models.MatchReference> matches = new List<RiotNet.Models.MatchReference>();

                    DateTime weekAgo = DateTime.UtcNow.AddDays(-7);
                    if (player.lastUpdated < weekAgo) //RIOT API has max span of 7 days, so if it's been over 7 days since we last updated, we have to do multiple smaller calls
                    {
                        DateTime dateTimePointer = player.lastUpdated;
                        DateTime endTimePointer = dateTimePointer.AddDays(7); //Defines our span from when the player was last updated to that time + 7 days
                        DateTime nowTime = DateTime.UtcNow; //We must cache this otherwise our goal moves as we go along

                        while (dateTimePointer < nowTime) //while our start day is less than UTC Time
                        {
                            endTimePointer = dateTimePointer.AddDays(7); //7day span define

                            if (endTimePointer > nowTime) //if the 7 day span EXCEEDS the current time then we are at the end and we just span until UTC now
                            {
                                endTimePointer = nowTime;
                            }

                            RiotNet.Models.MatchList matchList = await BotMain.riotInstance.GetMatchListByAccountIdAsync(
                                    player.summonerIDs[0],
                                    null,
                                    new List<RiotNet.Models.QueueType>() { RiotNet.Models.QueueType.TEAM_BUILDER_RANKED_SOLO },
                                    null,
                                    dateTimePointer,
                                    endTimePointer,
                                    null,
                                    null);

                            if (matchList != null && matchList.Matches != null) //The API gives you a 404 which gets saved as a null if empty list is returned
                            {
                                foreach (RiotNet.Models.MatchReference match in matchList.Matches)
                                {
                                    matches.Add(match);
                                }
                            }
                            dateTimePointer = endTimePointer.AddMilliseconds(1);
                        }
                    }
                    else
                    {

                        RiotNet.Models.MatchList matchList = await BotMain.riotInstance.GetMatchListByAccountIdAsync(
                            player.summonerIDs[0],
                            null,
                            new List<RiotNet.Models.QueueType>() { RiotNet.Models.QueueType.TEAM_BUILDER_RANKED_SOLO },
                            null,
                            player.lastUpdated,
                            null,
                            null,
                            null);

                        if (matchList != null && matchList.Matches != null) //The API gives you a 404 which gets saved as a null if empty list is returned
                        {
                            foreach (RiotNet.Models.MatchReference match in matchList.Matches)
                            {
                                matches.Add(match);
                            }
                        }
                    }

                    Console.WriteLine(string.Format("Found {0} games", matches.Count));

                    foreach (RiotNet.Models.MatchReference match in matches)
                    {
                        var game = await BotMain.riotInstance.GetMatchAsync(match.GameId);
                        int participantID = game.ParticipantIdentities.Find((participant) => participant.Player.AccountId == summonerID).ParticipantId;
                        var playerInfo = game.Participants.Find((participant) => participant.ParticipantId == participantID);
                        database.AddNewGame(
                            player.discordID, summonerID, match.GameId, game.GameCreation,
                            playerInfo.Stats.Win, playerInfo.Stats.Kills, playerInfo.Stats.Deaths, playerInfo.Stats.Assists, playerInfo.Stats.VisionScore, playerInfo.Stats.TotalMinionsKilled + playerInfo.Stats.NeutralMinionsKilled,
                            playerInfo.ChampionId);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.Write(ex.StackTrace);
                }
            }

            player.lastUpdated = DateTime.UtcNow;
        }

        static List<Game> FilterGamesTimeFrame(List<Game> games, TimeFrames timeFrame)
        {
            switch (timeFrame)
            {
                case TimeFrames.ALL:
                    return games;

                case TimeFrames.MONTH:
                    DateTime monthAgo = DateTime.Now.AddDays(-30);
                    List<Game> filteredGames = new List<Game>();
                    foreach (Game game in games)
                    {
                        if (game.played > monthAgo)
                        {
                            filteredGames.Add(game);
                        }
                    }
                    return filteredGames;

                case TimeFrames.WEEK:
                    DateTime weekAgo = DateTime.Now.AddDays(-7);
                    filteredGames = new List<Game>();
                    foreach (Game game in games)
                    {
                        if (game.played > weekAgo)
                        {
                            filteredGames.Add(game);
                        }
                    }
                    return filteredGames;
            }

            return games;
        }

        #region: Getting Stats
        static int GetScoreOfPlayer(PlayerObject player, TimeFrames timeFrame, Database database)
        {
            List<Game> games = FilterGamesTimeFrame(database.GetGamesOfUser(player.discordID), timeFrame);
            games.Sort((A, B) => A.played > B.played ? 1 : -1);
            HashSet<int> champsPlayedLast10 = new HashSet<int>();
            int score = 0;

            for (int i = 0; i < games.Count; i++)
            {
                Game game = games[i];
                int championPlayed = game.championPlayed;
                champsPlayedLast10.Add(championPlayed);

                if (game.win)
                {
                    score += 2;
                }
                else
                {
                    score += 1;
                }

                if (player.IsMain(game.summoner))
                {
                    score += 1;
                }

                if (i % 10 == 9)
                {
                    score -= (champsPlayedLast10.Count - 1);
                    champsPlayedLast10.Clear();
                }
            }
            Console.WriteLine(score);


            return score;
        }


        static int GetKillsOfPlayer(PlayerObject player, TimeFrames timeFrame, Database database)
        {
            List<Game> games = FilterGamesTimeFrame(database.GetGamesOfUser(player.discordID), timeFrame);

            int kills = 0;

            foreach(Game game in games)
            {
                kills += game.kills;
            }

            return kills;
        }

        static int GetDeathsOfPlayer(PlayerObject player, TimeFrames timeFrame, Database database)
        {
            List<Game> games = FilterGamesTimeFrame(database.GetGamesOfUser(player.discordID), timeFrame);

            int deaths = 0;

            foreach (Game game in games)
            {
                deaths += game.deaths;
            }

            return deaths;
        }

        static int GetAssistsOfPlayer(PlayerObject player, TimeFrames timeFrame, Database database)
        {
            List<Game> games = FilterGamesTimeFrame(database.GetGamesOfUser(player.discordID), timeFrame);

            int assissts = 0;

            foreach (Game game in games)
            {
                assissts += game.assists;
            }

            return assissts;
        }

        static int GetCSOfPlayer(PlayerObject player, TimeFrames timeFrame, Database database)
        {
            List<Game> games = FilterGamesTimeFrame(database.GetGamesOfUser(player.discordID), timeFrame);

            int cs = 0;

            foreach (Game game in games)
            {
                cs += game.creepScore;
            }

            return cs;
        }

        static int GetVisionScoreOfPlayer(PlayerObject player, TimeFrames timeFrame, Database database)
        {
            List<Game> games = FilterGamesTimeFrame(database.GetGamesOfUser(player.discordID), timeFrame);

            int vision = 0;

            foreach (Game game in games)
            {
                vision += game.visionScore;
            }

            return vision;
        }
        #endregion

        public static List<PlayerScore> CreateLeaderboard(LeaderboardSortTypes sortType, TimeFrames timeFrame, Database database)
        {
            List<PlayerScore> scores = new List<PlayerScore>();
            foreach(PlayerObject player in database.GetAllPlayers())
            {
                scores.Add(new PlayerScore(player));
            }

            Func<PlayerObject, TimeFrames, Database, int> scoreFetchFunc;

            switch(sortType)
            {
                case LeaderboardSortTypes.SCORE:
                    scoreFetchFunc = GetScoreOfPlayer;
                    break;

                case LeaderboardSortTypes.KILLS:
                    scoreFetchFunc = GetKillsOfPlayer;
                    break;

                case LeaderboardSortTypes.DEATHS:
                    scoreFetchFunc = GetDeathsOfPlayer;
                    break;

                case LeaderboardSortTypes.ASSISTS:
                    scoreFetchFunc = GetAssistsOfPlayer;
                    break;

                case LeaderboardSortTypes.CS:
                    scoreFetchFunc = GetCSOfPlayer;
                    break;

                case LeaderboardSortTypes.VISION:
                    scoreFetchFunc = GetVisionScoreOfPlayer;
                    break;

                default:
                    scoreFetchFunc = GetScoreOfPlayer;
                    break;

            }

            foreach(PlayerScore score in scores)
            {
                score.score = scoreFetchFunc(score.player, timeFrame, database);
            }

            scores.Sort((A, B) => A.score < B.score ? 1 : -1);

            return scores;
        }
    }
}
