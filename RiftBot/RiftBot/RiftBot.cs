using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiotNet;
using Discord.Commands;
using System.Reflection;

namespace RiftBot
{
    class RiftBot
    {
        public static void Main(string[] args)
            => new RiftBot().MainAsync().GetAwaiter().GetResult();

        public static CommandHandler handler;
        public static DiscordSocketClient client;
        public static IRiotClient riotInstance;

        public static Database database;
        System.Threading.Timer dbSaveTimer;
        System.Threading.Timer gameFetchTimer;


        public async Task MainAsync()
        {
            database = new Database();
            database.Load();

            RiotClient.DefaultPlatformId = RiotNet.Models.PlatformId.NA1;
            riotInstance = new RiotClient(new RiotClientSettings
            {
                ApiKey = Keys.RIOT_KEY
            });

            string bot_token = Keys.DISCORD_KEY;
            client = new DiscordSocketClient();

            handler = new CommandHandler(client, new CommandService());
            

            client.Log += Log;
            client.Ready += () =>
            {
                Console.WriteLine("Bot is connected!");
                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.Bot, bot_token);
            await client.StartAsync();

            await handler.InstallCommandsAsync();

            dbSaveTimer = new System.Threading.Timer(SaveDatabase, null, 1000, 1000 * 10); //10 seconds
            gameFetchTimer = new System.Threading.Timer(FetchGames, null, 1000, 1000 * 60 * 10); //10 minutes

            client.Disconnected += (evt) =>
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

        void FetchGames(object state)
        {
            List<Task> tasks = new List<Task>();
            foreach(PlayerObject player in database.GetAllPlayers())
            {
                Console.WriteLine(string.Format("Attempting to fetch games for {0}", player.discordID));
                tasks.Add(GetMatches.AddGamesToDatabase(database, player));
            }
        }

        Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }

    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('%', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            Console.WriteLine(">" + messageParam.Content);

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }
    }
}
