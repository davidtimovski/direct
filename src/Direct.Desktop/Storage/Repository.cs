using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Direct.Desktop.Storage.Entities;
using Direct.Desktop.Storage.Models;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace Direct.Desktop.Storage;

internal static class Repository
{
    private const string DatabaseFileName = "direct.db";
    private const int RecentMessagesLimit = 30;

    private static readonly string _connectionString;

    static Repository()
    {
        string dbFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);
        _connectionString = $"Data Source={dbFilePath}";
    }

    internal async static Task<List<ContactForView>> GetAllContactsAsync()
    {
        using var connection = OpenConnection();

        var contactsCmd = new SqliteCommand
        {
            Connection = connection,
            CommandText = "SELECT id, nickname FROM contacts"
        };

        var result = new List<ContactForView>();
        var lastMessageTsLookup = new Dictionary<Guid, DateTime>();

        SqliteDataReader contactsReader = await contactsCmd.ExecuteReaderAsync();
        while (contactsReader.Read())
        {
            var id = new Guid(contactsReader.GetString(0));

            result.Add(new ContactForView
            {
                Id = id,
                Nickname = contactsReader.GetString(1)
            });
            lastMessageTsLookup.Add(id, DateTime.MinValue);
        }

        // Order contacts by the timestamp of the last message the user had with them
        var lastMessagesCmd = new SqliteCommand
        {
            Connection = connection,
            CommandText = @"WITH ranked_messages AS (
                                SELECT m.*, ROW_NUMBER() OVER (PARTITION BY contact_id ORDER BY sent_at DESC) AS rn
                                FROM messages AS m
                            )
                            SELECT contact_id, sent_at FROM ranked_messages WHERE rn = 1 ORDER BY sent_at DESC"
        };

        SqliteDataReader lastMessagesReader = await lastMessagesCmd.ExecuteReaderAsync();
        while (lastMessagesReader.Read())
        {
            var id = new Guid(lastMessagesReader.GetString(0));
            lastMessageTsLookup[id] = Iso8601ToDateTime(lastMessagesReader.GetString(1));
        }

        return result.OrderByDescending(x => lastMessageTsLookup[x.Id]).ToList();
    }

    internal async static Task CreateContactAsync(Contact contact)
    {
        using var connection = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = connection,
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
        using var connection = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = connection,
            CommandText = "UPDATE contacts SET nickname = @nickname WHERE id = @id"
        };
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.Parameters.AddWithValue("@nickname", nickname);

        await cmd.ExecuteNonQueryAsync();
    }

    internal async static Task DeleteContactAsync(Guid id, bool deleteMessages)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var deleteContactsCmd = new SqliteCommand
        {
            Transaction = transaction,
            Connection = connection,
            CommandText = "DELETE FROM contacts WHERE id = @id"
        };
        deleteContactsCmd.Parameters.AddWithValue("@id", id.ToString());
        await deleteContactsCmd.ExecuteNonQueryAsync();

        if (deleteMessages)
        {
            var deleteMessagesCmd = new SqliteCommand
            {
                Transaction = transaction,
                Connection = connection,
                CommandText = "DELETE FROM messages WHERE contact_id = @userId"
            };
            deleteMessagesCmd.Parameters.AddWithValue("@userId", id.ToString());
            await deleteMessagesCmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    internal async static Task<List<Message>> GetRecentMessagesAsync(Guid contactId, DateTime? from)
    {
        var sql = from.HasValue
            ? $"SELECT * FROM messages WHERE contact_id = @contact_id AND sent_at >= @from ORDER BY sent_at DESC LIMIT {RecentMessagesLimit}"
            : $"SELECT * FROM messages WHERE contact_id = @contact_id ORDER BY sent_at DESC LIMIT {RecentMessagesLimit}";

        using var connection = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = connection,
            CommandText = sql
        };
        cmd.Parameters.AddWithValue("@contact_id", contactId.ToString());
        if (from.HasValue)
        {
            cmd.Parameters.AddWithValue("@from", DateTimeToIso8601(from.Value));
        }

        var result = new List<Message>();

        SqliteDataReader reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
            result.Add(new Message
            {
                Id = new Guid(reader.GetString(0)),
                ContactId = new Guid(reader.GetString(1)),
                IsRecipient = reader.GetBoolean(2),
                Text = reader.GetString(3),
                Reaction = reader.IsDBNull(4) ? null : reader.GetString(4),
                SentAt = Iso8601ToDateTime(reader.GetString(5)),
                EditedAt = reader.IsDBNull(6) ? null : Iso8601ToDateTime(reader.GetString(6))
            });
        }

        return result;
    }

    internal async static Task CreateMessageAsync(Message message)
    {
        using var connection = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = connection,
            CommandText = @"INSERT INTO messages (id, contact_id, is_recipient, message, reaction, sent_at, edited_at)
                            VALUES (@id, @contact_id, @is_recipient, @message, NULL, @sent_at, NULL)"
        };
        cmd.Parameters.AddWithValue("@id", message.Id.ToString());
        cmd.Parameters.AddWithValue("@contact_id", message.ContactId.ToString());
        cmd.Parameters.AddWithValue("@is_recipient", message.IsRecipient);
        cmd.Parameters.AddWithValue("@message", message.Text);
        cmd.Parameters.AddWithValue("@sent_at", DateTimeToIso8601(message.SentAt));

        await cmd.ExecuteNonQueryAsync();
    }

    internal async static Task UpdateMessageAsync(Guid id, string message, DateTime editedAt)
    {
        using var connection = OpenConnection();

        var cmd = new SqliteCommand
        {
            Connection = connection,
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

        using var connection = OpenConnection();

        var sql = @"CREATE TABLE IF NOT EXISTS contacts
                    (
                        id TEXT PRIMARY KEY,
                        nickname TEXT NOT NULL,
                        added_on TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS messages
                    (
                        id TEXT PRIMARY KEY,
                        contact_id TEXT NOT NULL,
                        is_recipient TINYINT NOT NULL,
                        message TEXT NOT NULL,
                        reaction TEXT,
                        sent_at TEXT NOT NULL,
                        edited_at TEXT
                    );

                    CREATE INDEX IF NOT EXISTS contact_id_ix 
                    ON messages(contact_id);

                    CREATE INDEX IF NOT EXISTS sent_at_ix 
                    ON messages(sent_at);";

        var cmd = new SqliteCommand(sql, connection);

        cmd.ExecuteNonQuery();
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
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
