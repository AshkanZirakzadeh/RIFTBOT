using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using RiftBot;

[Group("manage")]
public class ManageModule : ModuleBase<SocketCommandContext>
{
    [Command("register")]
    public async Task Register(string summonerID)
    {
        if (summonerID == null)
        {
            await ReplyAsync("Please supply a summonerID Ex: \"!manage register MYIGN\"");
        }
        else
        {
            bool addSuccess = false;

            try
            {
                var summoner = (await RiftBot.RiftBot.riotInstance.GetSummonerBySummonerNameAsync(summonerID, RiotNet.Models.PlatformId.NA1));
                addSuccess = RiftBot.RiftBot.database.AddNewUser(Context.Client.CurrentUser.Id, summoner.AccountId);

                if (addSuccess)
                {
                    await ReplyAsync("You have been registered");
                }
                else
                {
                    await ReplyAsync("Failed to register, you are either already registered or your summoner ID is already registered to someone else.");
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

