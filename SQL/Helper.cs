using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using System.Data;
using Microsoft.Data.Sqlite;
using PokeApiNet;

namespace DiscordBot.SQL
{
    internal class Helper
    {
        private readonly string _dbPath;

        public Helper(string? dbPath = null)
        {

            var basePath = AppContext.BaseDirectory;
            var projectRoot = Directory.GetParent(basePath)!.Parent!.Parent!.Parent!.FullName;
            dbPath = Path.Combine(projectRoot, "SQL", "helper.db");

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            _dbPath = dbPath;

            //Console.WriteLine(dbPath);
            InitializeDatabase();
        }
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            //DROP TABLE IF EXISTS CachePokemon;

            var command = connection.CreateCommand();
            command.CommandText = @"

CREATE TABLE IF NOT EXISTS PromptCache (
    HashedPrompt TEXT PRIMARY KEY,
    EncodedPrompt TEXT NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS GamblingCache (
    UserID TEXT PRIMARY KEY,
    Money DECIMAL(10, 2),
    LastUpdated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS JelqCache (
    JelqId INTEGER PRIMARY KEY AUTOINCREMENT,
    JelqDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    JelqAmount DOUBLE NOT NULL,
    JelqAmountTotal DOUBLE
);
CREATE TABLE IF NOT EXISTS CachePokemon (
    UserID INTEGER NOT NULL,
    PokeName TEXT NOT NULL,
    PokeAmt INTEGER DEFAULT 0,
    PokeCaught BOOLEAN DEFAULT 0,
    PokeImg TEXT,
    PRIMARY KEY (UserID, PokeName)
);
CREATE TABLE IF NOT EXISTS GirlsCache (
    UserID INTEGER NOT NULL,
    GirlInfo TEXT NOT NULL,
    LoveMeter DOUBLE(2, 2) DEFAULT 0
);         
            ";
            command.ExecuteNonQuery();
        }
        public void SaveJelq(DateTime date, double amt)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO JelqCache (JelqDate, JelqAmount, JelqAmountTotal)
VALUES ($date, $jelq, COALESCE((SELECT SUM(JelqAmount) FROM JelqCache), 0) + $jelq);
";
            command.Parameters.AddWithValue("$date", date);
            command.Parameters.AddWithValue("$jelq", amt);

            command.ExecuteNonQuery();

        }
        public double GetJelq()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
SELECT SUM(JelqAmount) AS JelqAmountTotal
FROM JelqCache;
";
            using var reader = command.ExecuteReader();
            return reader.Read() ? reader.GetDouble(0) : -1;
        }
        public void SavePokemon(ulong userId, string pokemon, int amount, bool caught = true, string? image = null)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO CachePokemon (UserID, PokeName, PokeAmt, PokeCaught, PokeImg)
VALUES ($id, $pk, $pka, $pkc, $pki)
ON CONFLICT(UserID, PokeName) DO UPDATE SET
PokeAmt = excluded.PokeAmt,
PokeCaught = excluded.PokeCaught,
PokeImg = excluded.PokeImg;
";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$pk", pokemon);
            command.Parameters.AddWithValue("$pka", amount);
            command.Parameters.AddWithValue("$pkc", caught ? 1 : 0); // SQLite uses 0/1 for booleans
            command.Parameters.AddWithValue("$pki", image ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }
        public List<(string name, int amount, bool caught, string img)> GetPokemon(ulong userId)
        {
            var pokemons = new List<(string name, int amount, bool caught, string img)>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
        SELECT SUBSTRING(PokeName, 1, 4) AS PokeID, PokeName, PokeAmt, PokeCaught, PokeImg
        FROM CachePokemon
        WHERE UserID = $id
        ORDER BY PokeID ASC;
    ";
            command.Parameters.AddWithValue("$id", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                var amt = reader.GetInt32(2);
                var caught = reader.GetBoolean(3);
                var img = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

                pokemons.Add((name, amt, caught, img));
            }

            return pokemons;
        }

        public void KillPokemon(ulong userId, string pokemon)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
DELETE FROM CachePokemon
WHERE UserID = $id AND PokeName LIKE '$pk%'; 
";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$pk", pokemon);


            command.ExecuteNonQuery();
        }
        public void SavePrompt(string hashedPrompt, string encodedPrompt)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
INSERT OR REPLACE INTO PromptCache (HashedPrompt, EncodedPrompt)
VALUES ($hash, $encoded);
";
            command.Parameters.AddWithValue("$hash", hashedPrompt);
            command.Parameters.AddWithValue("$encoded", encodedPrompt);

            command.ExecuteNonQuery();
        }
        public string? GetPrompt(string hashedPrompt)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
SELECT EncodedPrompt FROM PromptCache
WHERE HashedPrompt = $hash;
";
            command.Parameters.AddWithValue("$hash", hashedPrompt);

            using var reader = command.ExecuteReader();
            return reader.Read() ? reader.GetString(0) : null;
        }
        public void SaveMoney(ulong userID, decimal money)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO GamblingCache (UserID, Money)
VALUES ($user, $money)
ON CONFLICT(UserID) DO UPDATE SET Money = Money + $money;
";
            command.Parameters.AddWithValue("$user", userID);
            command.Parameters.AddWithValue("$money", Math.Round(money, 2));

            command.ExecuteNonQuery();
        }

        public decimal GetMoney(ulong userID)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Money FROM GamblingCache
WHERE UserID = $user;
";
            command.Parameters.AddWithValue("$user", userID);

            using var reader = command.ExecuteReader();
            return reader.Read() ? reader.GetDecimal(0) : 0;
        }

        public void SaveGirl (ulong userID, string info)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO GirlsCache (UserID, GirlInfo)
VALUES ($user, $info);
";
            command.Parameters.AddWithValue("$user", userID);
            command.Parameters.AddWithValue("$info", info);

            command.ExecuteNonQuery();

        }
        public List<(string info, double meter)> GetGirl(ulong userID)
        {
            var girls = new List<(string info, double meter)>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
SELECT GirlInfo, LoveMeter FROM GirlsCache
WHERE UserID = $user;
";
            command.Parameters.AddWithValue("$user", userID);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var info = reader.GetString(0);
                var meter = reader.GetDouble(1  );
               
                girls.Add((info, meter));
            }

            return girls;
        }
    }
}
