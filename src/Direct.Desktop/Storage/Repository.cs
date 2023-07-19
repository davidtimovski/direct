using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace Direct.Desktop.Storage;

internal static class Repository
{
    private const string DatabaseFileName = "direct.db";
    private static readonly string _connectionString;

    static Repository()
    {
        string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);
        _connectionString = $"Data Source={dbPath}";
    }

    internal async static Task CreateMessageAsync(Message message)
    {
        using var db = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = db,
            CommandText = @"INSERT INTO messages (id, sender_id, recipient_id, message, reaction, sent_at, edited_at)
                            VALUES (@id, @sender_id, @recipient_id, @message, NULL, @sent_at, NULL)"
        };
        cmd.Parameters.AddWithValue("@id", message.Id.ToString());
        cmd.Parameters.AddWithValue("@sender_id", message.SenderId.ToString());
        cmd.Parameters.AddWithValue("@recipient_id", message.RecipientId.ToString());
        cmd.Parameters.AddWithValue("@message", message.Text);
        cmd.Parameters.AddWithValue("@sent_at", DateTimeToIso8601(message.SentAt));

        await cmd.ExecuteNonQueryAsync();
    }

    internal async static Task UpdateMessageAsync(Guid id, string message, DateTime editedAt)
    {
        using var db = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = db,
            CommandText = "UPDATE messages SET message = @message, edited_at = @edited_at WHERE id = @id"
        };
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.Parameters.AddWithValue("@message", message);
        cmd.Parameters.AddWithValue("@edited_at", DateTimeToIso8601(editedAt));

        await cmd.ExecuteNonQueryAsync();
    }

    internal async static Task<List<Message>> GetAllMessagesAsync(List<Guid> userIds)
    {
        var cmd = new SqliteCommand();

        var parameters = new string[userIds.Count];
        for (int i = 0; i < userIds.Count; i++)
        {
            parameters[i] = $"@user_id{i}";
            cmd.Parameters.AddWithValue(parameters[i], userIds[i].ToString());
        }

        using var db = OpenConnection();

        cmd.CommandText = $"SELECT * FROM messages WHERE sender_id IN ({string.Join(", ", parameters)}) OR recipient_id IN ({string.Join(", ", parameters)}) ORDER BY sent_at";
        cmd.Connection = db;

        var messages = new List<Message>();

        SqliteDataReader query = await cmd.ExecuteReaderAsync();
        while (query.Read())
        {
            messages.Add(new Message
            {
                Id = new Guid(query.GetString(0)),
                SenderId = new Guid(query.GetString(1)),
                RecipientId = new Guid(query.GetString(2)),
                Text = query.GetString(3),
                Reaction = query.IsDBNull(4) ? null : query.GetString(4),
                SentAt = Iso8601ToDateTime(query.GetString(5)),
                EditedAt = query.IsDBNull(6) ? null : Iso8601ToDateTime(query.GetString(6))
            });
        }

        return messages;
    }

    internal async static void InitializeDatabaseAsync()
    {
        await ApplicationData.Current.LocalFolder.CreateFileAsync(DatabaseFileName, CreationCollisionOption.OpenIfExists);

        using var db = OpenConnection();

        var sql = @"CREATE TABLE IF NOT EXISTS messages
                    (
	                    id TEXT PRIMARY KEY,
	                    sender_id TEXT NOT NULL,
	                    recipient_id TEXT NOT NULL,
	                    message TEXT NOT NULL,
                        reaction TEXT,
	                    sent_at TEXT NOT NULL,
                        edited_at TEXT
                    );

                    CREATE INDEX sender_id_ix 
                    ON messages(sender_id);

                    CREATE INDEX recipient_id_ix 
                    ON messages(recipient_id);";

        var cmd = new SqliteCommand(sql, db);

        cmd.ExecuteNonQuery();
    }

    private static SqliteConnection OpenConnection()
    {
        var db = new SqliteConnection(_connectionString);
        db.Open();
        return db;
    }

    private static string DateTimeToIso8601(DateTime dateTime)
    {
        return dateTime.ToString("s", CultureInfo.InvariantCulture);
    }

    private static DateTime Iso8601ToDateTime(string iso8601)
    {
        return DateTime.ParseExact(iso8601, "s", CultureInfo.InvariantCulture);
    }
}
