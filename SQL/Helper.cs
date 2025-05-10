using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using System.Data;
using Microsoft.Data.Sqlite;

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
           
        ";
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

        public decimal GetMoneyDecimal(ulong userID)
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

        public string? GetEncodedPrompt(string hashedPrompt)
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

        public void saveJelq(DateTime date, double amt)
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

        public double getJelqTotal()
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
    }
}
