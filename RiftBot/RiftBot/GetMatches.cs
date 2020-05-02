using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiotNet;

namespace RiftBot
{
    public static class GetMatches
    {
        public async static Task AddGamesToDatabase(Database database, PlayerObject player)
        {
            foreach(string summonerID in player.summonerIDs)
            {

                try
                {
                    RiotNet.Models.MatchList matches = await RiftBot.riotInstance.GetMatchListByAccountIdAsync(
                                    player.summonerIDs[0],
                                    null,
                                    new List<RiotNet.Models.QueueType>() { RiotNet.Models.QueueType.TEAM_BUILDER_RANKED_SOLO },
                                    null,
                                    player.lastUpdated,
                                    null,
                                    null,
                                    null);

                    if (matches != null) //The API gives you a 404 which gets saved as a null if empty list is returned
                    {
                        Console.WriteLine(string.Format("Found {0} games", matches.TotalGames));

                        foreach (RiotNet.Models.MatchReference match in matches.Matches)
                        {
                            var game = await RiftBot.riotInstance.GetMatchAsync(match.GameId);
                            int participantID = game.ParticipantIdentities.Find((participant) => participant.Player.AccountId == summonerID).ParticipantId;
                            var playerInfo = game.Participants.Find((participant) => participant.ParticipantId == participantID);
                            database.AddNewGame(
                                player.discordID, match.GameId, game.GameCreation,
                                playerInfo.Stats.Win, playerInfo.Stats.Kills, playerInfo.Stats.Deaths, playerInfo.Stats.Assists, playerInfo.Stats.VisionScore, playerInfo.Stats.TotalMinionsKilled + playerInfo.Stats.NeutralMinionsKilled,
                                playerInfo.ChampionId);
                        }
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
    }
}
