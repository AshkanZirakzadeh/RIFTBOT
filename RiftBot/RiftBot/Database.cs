using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiftBot
{
    class PlayerObject
    {
        public int discordID; //primary key
        public DateTime created;
        public List<int> summonerIDs;
        public bool riftPlayer;
    }

    class Game
    {
        public int gameID;
        public DateTime played;
        public bool win;
        public int kills;
        public int deaths;
        public int assists;
        public int visionScore;
        public int championPlayed;
    }

    class DatabaseStore
    {
        public Dictionary<int, PlayerObject> players;
        public Dictionary<int, List<Game>> games;
    }

    class Database
    {
        const string filePath = @"Database.json";
        Dictionary<int, PlayerObject> players;
        Dictionary<int, List<Game>> games;

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
                players = new Dictionary<int, PlayerObject>();
                games = new Dictionary<int, List<Game>>();
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

        //Adds a new user to the database
        //Returns false if failed due to existing IDs
        public bool AddNewUser(int discordID, int summonerID)
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
                summonerIDs = new List<int>() { summonerID }
            };

            players.Add(newUser.discordID, newUser);
            games.Add(newUser.discordID, new List<Game>());

            return true;
        }

        //Adds information for a game to the database
        //Returns false if adding failed due to game already being in the database, and true otherwise
        public bool AddNewGame(int discordID, int GameID, DateTime played, bool win, int kills, int deaths, int assists, int visionScore, int champion)
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
                win = win,
                kills = kills,
                deaths = deaths,
                assists = assists,
                visionScore = visionScore,
                championPlayed = champion
            };

            games[player.discordID].Add(newGame);

            return true;
        }

        public List<Game> GetGamesOfUser(int discordID)
        {
            if (!players.ContainsKey(discordID))
            {
                return null;
            }

            return games[discordID];
        }

        public List<PlayerObject> GetAllPlayers()
        {
            return new List<PlayerObject>(players.Values);
        }

        public void SetPlayerStatus(int discordID, bool status)
        {
            if (!players.ContainsKey(discordID))
            {
                return;
            }

            players[discordID].riftPlayer = status;
        }

        //returns false if failed due to existing ID/not registered
        public bool AddNewSummonerID(int discordID, int summonerID)
        {
            if (!players.ContainsKey(discordID))
            {
                return false;
            }

            if (CheckSummonerExists(summonerID))
            {
                return false;
            }

            players[discordID].summonerIDs.Add(summonerID);

            return true;
        }

        //Checks if a summoner ID is assosciated with ANY account
        //Returns true if the ID already assosciated
        bool CheckSummonerExists(int summonerID)
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
