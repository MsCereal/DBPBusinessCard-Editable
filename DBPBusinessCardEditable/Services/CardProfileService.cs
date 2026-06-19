using DBPBusinessCardEditable.Models;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace DBPBusinessCardEditable.Services
{
    /// <summary>
    /// SQLite-backed store keyed by Employee ID.
    /// Data survives restarts. Single .db file stored in /data/ (Railway) or app root.
    /// </summary>
    public class CardProfileService
    {
        private readonly string _connectionString;

        public CardProfileService()
        {
            // Use /data/ on Railway (persistent volume), fall back to app directory locally
            string dir = Directory.Exists("/data") ? "/data" : AppContext.BaseDirectory;
            string dbPath = Path.Combine(dir, "cards.db");
            _connectionString = $"Data Source={dbPath}";
            InitDb();
        }

        private void InitDb()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Cards (
                    EmpId       TEXT PRIMARY KEY,
                    Name        TEXT NOT NULL DEFAULT '',
                    Title       TEXT NOT NULL DEFAULT '',
                    Org         TEXT NOT NULL DEFAULT '',
                    Phone       TEXT NOT NULL DEFAULT '',
                    Email       TEXT NOT NULL DEFAULT '',
                    GitHub      TEXT NOT NULL DEFAULT '',
                    LinkedIn    TEXT NOT NULL DEFAULT '',
                    Portfolio   TEXT NOT NULL DEFAULT '',
                    Photo       TEXT NOT NULL DEFAULT '',
                    Office      TEXT NOT NULL DEFAULT '',
                    LastUpdated TEXT NOT NULL DEFAULT ''
                );";
            cmd.ExecuteNonQuery();

            // Add Office column if upgrading from older schema
            try
            {
                cmd.CommandText = "ALTER TABLE Cards ADD COLUMN Office TEXT NOT NULL DEFAULT '';";
                cmd.ExecuteNonQuery();
            }
            catch { /* column already exists */ }
        }

        public CardProfile Get(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId)) return null;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Cards WHERE EmpId = @id";
            cmd.Parameters.AddWithValue("@id", empId.Trim());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return ReadProfile(reader);
        }

        public CardProfile GetOrCreate(string empId)
        {
            var existing = Get(empId);
            if (existing != null) return existing;
            return new CardProfile { EmpId = empId.Trim() };
        }

        public void Save(CardProfile profile)
        {
            profile.EmpId = profile.EmpId.Trim();
            profile.LastUpdated = DateTime.UtcNow;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Cards (EmpId, Name, Title, Org, Phone, Email, GitHub, LinkedIn, Portfolio, Photo, Office, LastUpdated)
                VALUES (@EmpId, @Name, @Title, @Org, @Phone, @Email, @GitHub, @LinkedIn, @Portfolio, @Photo, @Office, @LastUpdated)
                ON CONFLICT(EmpId) DO UPDATE SET
                    Name=@Name, Title=@Title, Org=@Org,
                    Phone=@Phone, Email=@Email,
                    GitHub=@GitHub, LinkedIn=@LinkedIn, Portfolio=@Portfolio,
                    Photo=@Photo, Office=@Office, LastUpdated=@LastUpdated;";
            AddParams(cmd, profile);
            cmd.ExecuteNonQuery();
        }

        public void Reset(string empId)
        {
            var blank = new CardProfile { EmpId = empId.Trim() };
            Save(blank);
        }

        /// <summary>Delete ALL cards — for demo cleanup.</summary>
        public int ClearAll()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Cards;";
            return cmd.ExecuteNonQuery();
        }

        /// <summary>Count of cards currently stored.</summary>
        public int Count()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Cards;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ── Helpers ──────────────────────────────────────────
        private static CardProfile ReadProfile(SqliteDataReader r) => new CardProfile
        {
            EmpId      = r["EmpId"]?.ToString() ?? "",
            Name       = r["Name"]?.ToString() ?? "",
            Title      = r["Title"]?.ToString() ?? "",
            Org        = r["Org"]?.ToString() ?? "",
            Phone      = r["Phone"]?.ToString() ?? "",
            Email      = r["Email"]?.ToString() ?? "",
            GitHub     = r["GitHub"]?.ToString() ?? "",
            LinkedIn   = r["LinkedIn"]?.ToString() ?? "",
            Portfolio  = r["Portfolio"]?.ToString() ?? "",
            Photo      = r["Photo"]?.ToString() ?? "",
            Office     = r["Office"]?.ToString() ?? "",
            LastUpdated = DateTime.TryParse(r["LastUpdated"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
        };

        private static void AddParams(SqliteCommand cmd, CardProfile p)
        {
            cmd.Parameters.AddWithValue("@EmpId",       p.EmpId);
            cmd.Parameters.AddWithValue("@Name",        p.Name ?? "");
            cmd.Parameters.AddWithValue("@Title",       p.Title ?? "");
            cmd.Parameters.AddWithValue("@Org",         p.Org ?? "");
            cmd.Parameters.AddWithValue("@Phone",       p.Phone ?? "");
            cmd.Parameters.AddWithValue("@Email",       p.Email ?? "");
            cmd.Parameters.AddWithValue("@GitHub",      p.GitHub ?? "");
            cmd.Parameters.AddWithValue("@LinkedIn",    p.LinkedIn ?? "");
            cmd.Parameters.AddWithValue("@Portfolio",   p.Portfolio ?? "");
            cmd.Parameters.AddWithValue("@Photo",       p.Photo ?? "");
            cmd.Parameters.AddWithValue("@Office",      p.Office ?? "");
            cmd.Parameters.AddWithValue("@LastUpdated", p.LastUpdated.ToString("o"));
        }
    }
}
