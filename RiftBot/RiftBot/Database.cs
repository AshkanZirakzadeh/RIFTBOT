using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiftBot
{
    public class PlayerObject
    {
        public ulong discordID; //primary key
        public DateTime created;
        public DateTime lastUpdated;
        public List<string> summonerIDs;
        public bool riftPlayer;
    }

    public class Game
    {
        public long gameID;
        public DateTime played;
        public bool win;
        public int kills;
        public int deaths;
        public int assists;
        public long visionScore;
        public int creepScore;
        public int championPlayed;
    }

    class DatabaseStore
    {
        public Dictionary<ulong, PlayerObject> players;
        public Dictionary<ulong, List<Game>> games;
    }

    public class Database
    {
        private readonly object databaseLock = new object();

        const string filePath = @"Database.json";
        Dictionary<ulong, PlayerObject> players;
        Dictionary<ulong, List<Game>> games;

        public void Load()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                DatabaseStore storageObject = JsonConvert.DeserializeObject<DatabaseStore>(json);
                players = storageObject.players;
                games = storageObject.games;
            }
            else
            {
                players = new Dictionary<ulong, PlayerObject>();
                games = new Dictionary<ulong, List<Game>>();
            }
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(
                new DatabaseStore()
                {
                    games = games,
                    players = players
                });

            File.WriteAllText(filePath, json);
        }

        public bool DiscordUserExists(ulong discordID)
        {
            return players.ContainsKey(discordID);
        }

        //Adds a new user to the database
        //Returns false if failed due to existing IDs
        public bool AddNewUser(ulong discordID, string summonerID)
        {
            if (players.ContainsKey(discordID))
            {
                throw new NullReferenceException(string.Format("Player with discord ID {0} already exists", discordID));
            }

            if (CheckSummonerExists(summonerID))
            {
                return false;
            }

            PlayerObject newUser = new PlayerObject()
            {
                discordID = discordID,
                created = DateTime.UtcNow,
                lastUpdated = DateTime.UtcNow,
                summonerIDs = new List<string>() { summonerID }
            };

            lock(databaseLock)
            {
                players.Add(newUser.discordID, newUser);
                games.Add(newUser.discordID, new List<Game>());
            }

            return true;
        }

        //Adds information for a game to the database
        //Returns false if adding failed due to game already being in the database, and true otherwise
        public bool AddNewGame(ulong discordID, long GameID, DateTime played, bool win, int kills, int deaths, int assists, long visionScore, int creepScore, int champion)
        {
            PlayerObject player = players[discordID];

            if (player == null)
            {
                throw new NullReferenceException(string.Format("Cannot find player of ID {0}", discordID));
            }

            if (games[player.discordID].Find((game) => game.gameID == GameID) != null)
            {
                return false;
            }

            Game newGame = new Game()
            {
                gameID = GameID,
                played = played,
                win = win,
                kills = kills,
                deaths = deaths,
                assists = assists,
                visionScore = visionScore,
                creepScore = creepScore,
                championPlayed = champion
            };
            lock (databaseLock)
            {
                games[player.discordID].Add(newGame);
            }

            return true;
        }

        public List<Game> GetGamesOfUser(ulong discordID)
        {
            if (!players.ContainsKey(discordID))
            {
                return null;
            }
            lock (databaseLock)
            {
                return games[discordID];
            }
        }

        public List<PlayerObject> GetAllPlayers()
        {
            lock (databaseLock)
            {
                return new List<PlayerObject>(players.Values);
            }
        }

        public void SetPlayerStatus(ulong discordID, bool status)
        {
            if (!players.ContainsKey(discordID))
            {
                return;
            }

            lock (databaseLock)
            {
                players[discordID].riftPlayer = status;
            }
                
        }

        //returns false if failed due to existing ID/not registered
        public bool AddNewSummonerID(ulong discordID, string summonerID)
        {
            if (!players.ContainsKey(discordID))
            {
                return false;
            }

            if (CheckSummonerExists(summonerID))
            {
                return false;
            }
            lock (databaseLock)
            {
                players[discordID].summonerIDs.Add(summonerID);
            }

            return true;
        }

        //Checks if a summoner ID is assosciated with ANY account
        //Returns true if the ID already assosciated
        bool CheckSummonerExists(string summonerID)
        {
            foreach(PlayerObject player in players.Values)
            {
                if (player.summonerIDs.Contains(summonerID))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
