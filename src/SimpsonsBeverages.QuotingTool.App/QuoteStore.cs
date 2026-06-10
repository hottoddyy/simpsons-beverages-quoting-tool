using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;

namespace SimpsonsBeverages.QuotingTool.App;

public sealed class QuoteStore
{
    private const string DbPath = @"\\adserver2\Company Share\Sales\Todds tools\quotes.db";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public bool IsAvailable()
    {
        try { return Directory.Exists(Path.GetDirectoryName(DbPath)); }
        catch { return false; }
    }

    public void Initialise()
    {
        if (!IsAvailable()) return;
        using var conn = Open();
        Exec(conn, """
            CREATE TABLE IF NOT EXISTS quotes (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                quote_number  TEXT    NOT NULL UNIQUE,
                year          INTEGER NOT NULL,
                sequence      INTEGER NOT NULL,
                customer      TEXT    NOT NULL,
                created_at    TEXT    NOT NULL,
                modified_at   TEXT    NOT NULL,
                data          TEXT    NOT NULL
            );
            CREATE TABLE IF NOT EXISTS counters (
                year          INTEGER PRIMARY KEY,
                last_sequence INTEGER NOT NULL DEFAULT 0
            );
            """);

        // Ensure the global sequence counter (year = 0) exists and is at least
        // as high as the maximum sequence already stored.  INSERT OR IGNORE creates
        // the row on first run; the UPDATE then bumps it up if an existing row is
        // lower than the actual max (repairs any silent-failure state).
        Exec(conn, "INSERT OR IGNORE INTO counters(year, last_sequence) VALUES (0, 0)");
        Exec(conn, """
            UPDATE counters
               SET last_sequence = (SELECT COALESCE(MAX(sequence), 0) FROM quotes)
             WHERE year = 0
               AND last_sequence < (SELECT COALESCE(MAX(sequence), 0) FROM quotes);
            """);
    }

    public string Save(string? existingNumber, QuoteStateSnapshot state)
    {
        var now = DateTime.UtcNow.ToString("o");
        var json = JsonSerializer.Serialize(state, JsonOptions);

        using var conn = Open();

        if (existingNumber is not null)
        {
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE quotes SET customer=$c, modified_at=$m, data=$d WHERE quote_number=$n";
            cmd.Parameters.AddWithValue("$c", state.Customer);
            cmd.Parameters.AddWithValue("$m", now);
            cmd.Parameters.AddWithValue("$d", json);
            cmd.Parameters.AddWithValue("$n", existingNumber);
            cmd.ExecuteNonQuery();
            tx.Commit();
            return existingNumber;
        }

        // BEGIN IMMEDIATE acquires the write lock before reading the counter,
        // preventing two concurrent sessions from being assigned the same number.
        // year = 0 is the global counter — the sequence never resets, so every
        // quote number is unique regardless of which year it was created in.
        Exec(conn, "BEGIN IMMEDIATE");
        try
        {
            var year = DateTime.Today.Year;

            using var counterCmd = conn.CreateCommand();
            counterCmd.CommandText = """
                UPDATE counters SET last_sequence = last_sequence + 1 WHERE year = 0
                RETURNING last_sequence
                """;
            var raw = counterCmd.ExecuteScalar();
            if (raw is null or DBNull)
                throw new InvalidOperationException(
                    "Quote store counter is missing — call Initialise() before Save().");
            var sequence = Convert.ToInt32(raw);
            var quoteNumber = $"SB-{year}-{sequence:D4}";

            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO quotes(quote_number, year, sequence, customer, created_at, modified_at, data)
                VALUES($n, $y, $s, $c, $ca, $m, $d)
                """;
            insertCmd.Parameters.AddWithValue("$n", quoteNumber);
            insertCmd.Parameters.AddWithValue("$y", year);
            insertCmd.Parameters.AddWithValue("$s", sequence);
            insertCmd.Parameters.AddWithValue("$c", state.Customer);
            insertCmd.Parameters.AddWithValue("$ca", now);
            insertCmd.Parameters.AddWithValue("$m", now);
            insertCmd.Parameters.AddWithValue("$d", json);
            insertCmd.ExecuteNonQuery();

            Exec(conn, "COMMIT");
            return quoteNumber;
        }
        catch
        {
            try { Exec(conn, "ROLLBACK"); } catch { /* ignore */ }
            throw;
        }
    }

    public QuoteStoreEntry? Load(string quoteNumber)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT quote_number, customer, created_at, modified_at, data FROM quotes WHERE quote_number=$n";
        cmd.Parameters.AddWithValue("$n", quoteNumber);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var state = JsonSerializer.Deserialize<QuoteStateSnapshot>(reader.GetString(4), JsonOptions);
        if (state is null) return null;

        return new QuoteStoreEntry(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            state);
    }

    public IReadOnlyList<QuoteStoreSummary> List(string? search = null)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        if (string.IsNullOrWhiteSpace(search))
        {
            cmd.CommandText = "SELECT quote_number, customer, modified_at FROM quotes ORDER BY id DESC LIMIT 300";
        }
        else
        {
            cmd.CommandText = """
                SELECT quote_number, customer, modified_at FROM quotes
                WHERE quote_number LIKE $s OR customer LIKE $s
                ORDER BY id DESC LIMIT 300
                """;
            cmd.Parameters.AddWithValue("$s", $"%{search}%");
        }

        var results = new List<QuoteStoreSummary>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            results.Add(new QuoteStoreSummary(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        return results;
    }

    // Opens a connection with a 5-second busy timeout so all operations
    // (reads and writes) wait gracefully under concurrent access.
    private static SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA busy_timeout = 5000";
        pragma.ExecuteNonQuery();
        return conn;
    }

    private static void Exec(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

public sealed record QuoteStoreEntry(
    string QuoteNumber,
    string Customer,
    string CreatedAt,
    string ModifiedAt,
    QuoteStateSnapshot State);

public sealed record QuoteStoreSummary(
    string QuoteNumber,
    string Customer,
    string ModifiedAt);
