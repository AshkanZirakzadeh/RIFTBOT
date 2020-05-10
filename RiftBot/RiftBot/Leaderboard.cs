using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeaderboardBot;

[Group("leaderboard")]
public class Leaderboard : ModuleBase<SocketCommandContext>
{
    [Command("")]
    public async Task Score(string sortArg = "score", string timeArg = "all")
    {
        string timeArgF = timeArg.ToLower();
        string sortArgF = sortArg.ToLower();

        TimeFrames timeFrame = TimeFrames.ALL;
        LeaderboardSortTypes leaderboardType = LeaderboardSortTypes.SCORE;
        if (timeArgF == "week" || timeArgF == "weekly")
        {
            timeFrame = TimeFrames.WEEK;
        }
        if (timeArgF == "month" || timeArgF == "monthly")
        {
            timeFrame = TimeFrames.MONTH;
        }

        if (sortArgF == "kill" || sortArgF == "kills")
        {
            leaderboardType = LeaderboardSortTypes.KILLS;
        }
        else if (sortArgF == "death" || sortArgF == "deaths")
        {
            leaderboardType = LeaderboardSortTypes.DEATHS;
        }
        else if (sortArgF == "assist" || sortArgF == "assists")
        {
            leaderboardType = LeaderboardSortTypes.ASSISTS;
        }
        else if (sortArgF == "vision" || sortArgF == "visionscore")
        {
            leaderboardType = LeaderboardSortTypes.VISION;
        }
        else if (sortArgF == "creep" || sortArgF == "creepscore" || sortArgF == "cs")
        {
            leaderboardType = LeaderboardSortTypes.CS;
        }
        
        List<PlayerScore> orderedList = GameUtility.CreateLeaderboard(leaderboardType, timeFrame, BotMain.database);
        int counter = 1;
        Console.WriteLine(leaderboardType);
        foreach(PlayerScore player in orderedList)
        {
            await ReplyAsync(counter++ + ". " + Context.Client.GetUser(player.player.discordID).Username + " - " + player.score);
            if (counter > 10)
            {
                break;
            }
        }
    }
}