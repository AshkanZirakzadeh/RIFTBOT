using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using LeaderboardBot;

[Group("manage")]
public class ManageModule : ModuleBase<SocketCommandContext>
{
    [Command("register")]
    public async Task Register(string summonerIDArg)
    {
        if (summonerIDArg == null)
        {
            await ReplyAsync("Please supply a summonerID Ex: \"!manage register MYIGN\"");
        }
        else if (LeaderboardBot.BotMain.database.DiscordUserExists(Context.User.Id))
        {
            await ReplyAsync("You are already registered");
        }
        else
        {
            bool addSuccess = false;

            try
            {
                var summoner = (await LeaderboardBot.BotMain.riotInstance.GetSummonerBySummonerNameAsync(summonerIDArg));
                addSuccess = LeaderboardBot.BotMain.database.AddNewUser(Context.User.Id, summoner.AccountId);

                if (addSuccess)
                {
                    await ReplyAsync("You have been registered");
                }
                else
                {
                    await ReplyAsync("Failed to register, your summoner ID is already registered to someone");
                }

            }
            catch (Exception ex)
            {
                await ReplyAsync("Failed to reach Riot API");
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
            }

        }
    }
}

