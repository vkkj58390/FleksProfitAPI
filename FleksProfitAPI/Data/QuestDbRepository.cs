using Npgsql;
using NpgsqlTypes;
using FleksProfitAPI.Models;

namespace FleksProfitAPI.Data
{
    public class QuestDbRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public QuestDbRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        // Retry opening the connection to tolerate QuestDB startup races
        private static async Task<NpgsqlConnection> OpenWithRetryAsync(NpgsqlDataSource ds, CancellationToken ct)
        {
            const int maxAttempts = 6;
            var delay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await ds.OpenConnectionAsync(ct);
                }
                catch (NpgsqlException) when (attempt < maxAttempts)
                {
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 10));
                }
            }

            // Final attempt: surface the real error
            return await ds.OpenConnectionAsync(ct);
        }

        // Helper to create timestamp parameter with Unspecified kind
        private static NpgsqlParameter CreateTs(string name, DateTime dt)
        {
            // QuestDB only supports TIMESTAMP (not timestamptz)
            return new NpgsqlParameter(name, NpgsqlDbType.Timestamp)
            {
                Value = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified)
            };
        }

        // Ensure table exists with proper QuestDB timestamp definition
        public async Task EnsureTableExistsAsync(CancellationToken ct = default)
        {
            const string sql = @"
            CREATE TABLE IF NOT EXISTS fcrrecords (
                hourutc TIMESTAMP,
                hourdk TIMESTAMP,
                fcrdomestic_mw DOUBLE,
                fcrabroad_mw DOUBLE,
                fcrcross_eur DOUBLE,
                fcrcross_dkk DOUBLE,
                fcrdk_eur DOUBLE,
                fcrdk_dkk DOUBLE
            )
            TIMESTAMP(hourutc)
            PARTITION BY DAY;";

            await using var conn = await OpenWithRetryAsync(_dataSource, ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        // Insert new records
        public async Task<int> InsertFcrRecordsAsync(IEnumerable<FcrRecord> records, CancellationToken ct = default)
        {
            const string sql = @"
            INSERT INTO fcrrecords
            (hourutc, hourdk, fcrdomestic_mw, fcrabroad_mw, fcrcross_eur, fcrcross_dkk, fcrdk_eur, fcrdk_dkk)
            VALUES (@hourutc, @hourdk, @fcrdomestic_mw, @fcrabroad_mw, @fcrcross_eur, @fcrcross_dkk, @fcrdk_eur, @fcrdk_dkk);";

            var count = 0;
            await using var conn = await OpenWithRetryAsync(_dataSource, ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            foreach (var r in records)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.Add(CreateTs("@hourutc", r.HourUTC));
                cmd.Parameters.Add(CreateTs("@hourdk",  r.HourDK));
                cmd.Parameters.AddWithValue("@fcrdomestic_mw", (object?)r.FCRdomestic_MW ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fcrabroad_mw",   (object?)r.FCRabroad_MW   ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fcrcross_eur",   (object?)r.FCRcross_EUR   ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fcrcross_dkk",   (object?)r.FCRcross_DKK   ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fcrdk_eur",      (object?)r.FCRdk_EUR      ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fcrdk_dkk",      (object?)r.FCRdk_DKK      ?? DBNull.Value);

                count += await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return count;
        }

        // Read records between two UTC timestamps
        public async Task<List<FcrRecord>> GetFcrRecordsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
        {
            const string sql = @"
            SELECT hourutc, hourdk, fcrdomestic_mw, fcrabroad_mw, fcrcross_eur, fcrcross_dkk, fcrdk_eur, fcrdk_dkk
            FROM fcrrecords
            WHERE hourutc BETWEEN @start AND @end
            ORDER BY hourutc;";

            var list = new List<FcrRecord>();
            await using var conn = await OpenWithRetryAsync(_dataSource, ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add(CreateTs("@start", startUtc));
            cmd.Parameters.Add(CreateTs("@end",   endUtc));

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new FcrRecord
                {
                    HourUTC = reader.GetDateTime(0),
                    HourDK = reader.GetDateTime(1),
                    FCRdomestic_MW = reader.IsDBNull(2) ? null : reader.GetDouble(2),
                    FCRabroad_MW   = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                    FCRcross_EUR   = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                    FCRcross_DKK   = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                    FCRdk_EUR      = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                    FCRdk_DKK      = reader.IsDBNull(7) ? null : reader.GetDouble(7)
                });
            }

            return list;
        }

        public async Task<DateTime?> GetLastHourUtcAsync(CancellationToken ct = default)
        {
            const string sql = "SELECT max(hourutc) FROM fcrrecords;";
            await using var conn = await OpenWithRetryAsync(_dataSource, ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result == null || result is DBNull ? null : (DateTime)result;
        }
    }
}