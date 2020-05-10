using Discord.Commands;
using LeaderboardBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[RequireUserPermission(Discord.GuildPermission.Administrator, Group = "Permission")]
[RequireOwner(Group = "Permission")]
[Group("admin")]
public class Admin : ModuleBase<SocketCommandContext>
{
    [Command("update")]
    public async Task UpdateGames()
    {
        BotMain.botInstance.FetchGames(null);

        await ReplyAsync("Force updated games");
    }

    [Command("backup")]
    public async Task BackupDB()
    {
        try
        {
            BotMain.database.Backup();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
        
        await ReplyAsync("Backed up DB");
    }

}
