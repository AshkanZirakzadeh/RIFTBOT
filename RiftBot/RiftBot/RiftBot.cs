using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiftBot
{
    class RiftBot
    {
        public static void Main(string[] args)
            => new RiftBot().MainAsync().GetAwaiter().GetResult();

        public DiscordSocketClient _client;
        public Database database;
        public System.Threading.Timer timer;

        public async Task MainAsync()
        {
            database = new Database();
            database.Load();

            timer = new System.Threading.Timer(SaveDatabase, null, 1000, 1000 * 10);

            string bot_token = Keys.DISCORD_KEY;

            _client = new DiscordSocketClient();

            _client.Log += Log;
            _client.Ready += () =>
            {
                Console.WriteLine("Bot is connected!");
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, bot_token);
            await _client.StartAsync();

            _client.Disconnected += (evt) =>
            {
                database.Save();
                return Task.CompletedTask;
            };

            await Task.Delay(-1);
        }

        void SaveDatabase(object state)
        {
            database.Save();
        }

        Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
