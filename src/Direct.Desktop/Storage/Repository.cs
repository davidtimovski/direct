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
        string dbFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);
        _connectionString = $"Data Source={dbFilePath}";
    }

    internal async static Task<List<Contact>> GetAllContactsAsync()
    {
        using var db = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = db,
            CommandText = "SELECT * FROM contacts"
        };

        var result = new List<Contact>();

        SqliteDataReader query = await cmd.ExecuteReaderAsync();
        while (query.Read())
        {
            result.Add(new Contact
            {
                Id = new Guid(query.GetString(0)),
                Nickname = query.GetString(1),
                AddedOn = Iso8601ToDateTime(query.GetString(2))
            });
        }

        return result;
    }

    internal async static Task CreateContactAsync(Contact contact)
    {
        using var db = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = db,
            CommandText = @"INSERT INTO contacts (id, nickname, added_on)
                            VALUES (@id, @nickname, @added_on)"
        };
        cmd.Parameters.AddWithValue("@id", contact.Id.ToString());
        cmd.Parameters.AddWithValue("@nickname", contact.Nickname);
        cmd.Parameters.AddWithValue("@added_on", DateTimeToIso8601(contact.AddedOn));

        await cmd.ExecuteNonQueryAsync();
    }

    internal async static Task UpdateContactAsync(Guid id, string nickname)
    {
        using var db = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = db,
            CommandText = "UPDATE contacts SET nickname = @nickname WHERE id = @id"
        };
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.Parameters.AddWithValue("@nickname", nickname);

        await cmd.ExecuteNonQueryAsync();
    }

    internal async static Task DeleteContactAsync(Guid id, bool deleteMessages)
    {
        using var db = OpenConnection();
        using var transaction = db.BeginTransaction();

        var deleteContactsCmd = new SqliteCommand
        {
            Transaction = transaction,
            Connection = db,
            CommandText = "DELETE FROM contacts WHERE id = @id"
        };
        deleteContactsCmd.Parameters.AddWithValue("@id", id.ToString());
        await deleteContactsCmd.ExecuteNonQueryAsync();

        if (deleteMessages)
        {
            var deleteMessagesCmd = new SqliteCommand
            {
                Transaction = transaction,
                Connection = db,
                CommandText = "DELETE FROM messages WHERE sender_id = @userId OR recipient_id = @userId"
            };
            deleteMessagesCmd.Parameters.AddWithValue("@userId", id.ToString());
            await deleteMessagesCmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    internal async static Task<List<Message>> GetMessagesAsync(Guid userId)
    {
        using var db = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = db,
            CommandText = "SELECT * FROM messages WHERE sender_id = @userId OR recipient_id = @userId ORDER BY sent_at"
        };
        cmd.Parameters.AddWithValue("@userId", userId.ToString());

        var result = new List<Message>();

        SqliteDataReader query = await cmd.ExecuteReaderAsync();
        while (query.Read())
        {
            result.Add(new Message
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

        return result;
    }

    internal async static Task<List<Message>> GetAllMessagesAsync()
    {
        var db = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = db,
            CommandText = "SELECT * FROM messages ORDER BY sent_at"
        };

        var result = new List<Message>();

        SqliteDataReader query = await cmd.ExecuteReaderAsync();
        while (query.Read())
        {
            result.Add(new Message
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

        return result;
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

    internal async static Task InitializeDatabaseAsync()
    {
        await ApplicationData.Current.LocalFolder.CreateFileAsync(DatabaseFileName, CreationCollisionOption.OpenIfExists);

        using var db = OpenConnection();

        var sql = @"CREATE TABLE IF NOT EXISTS contacts
                    (
	                    id TEXT PRIMARY KEY,
	                    nickname TEXT NOT NULL,
	                    added_on TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS messages
                    (
	                    id TEXT PRIMARY KEY,
	                    sender_id TEXT NOT NULL,
	                    recipient_id TEXT NOT NULL,
	                    message TEXT NOT NULL,
                        reaction TEXT,
	                    sent_at TEXT NOT NULL,
                        edited_at TEXT
                    );

                    CREATE INDEX IF NOT EXISTS sender_id_ix 
                    ON messages(sender_id);

                    CREATE INDEX IF NOT EXISTS recipient_id_ix 
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
