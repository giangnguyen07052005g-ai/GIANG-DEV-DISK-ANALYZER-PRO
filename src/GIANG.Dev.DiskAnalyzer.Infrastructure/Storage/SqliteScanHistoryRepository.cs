using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Storage;

public class SqliteScanHistoryRepository : IScanHistoryRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteScanHistoryRepository>? _logger;
    private readonly SemaphoreSlim _dbLock = new(1, 1);

    public SqliteScanHistoryRepository(string? dbPath = null, ILogger<SqliteScanHistoryRepository>? logger = null)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(dbPath))
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GiangDevDiskAnalyzer");
            Directory.CreateDirectory(appData);
            dbPath = Path.Combine(appData, "scanhistory.db");
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            string ddl = @"
                CREATE TABLE IF NOT EXISTS ScanHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ScanType INTEGER NOT NULL,
                    TargetPath TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL,
                    DurationMs INTEGER NOT NULL,
                    TotalFiles INTEGER NOT NULL,
                    TotalSizeBytes INTEGER NOT NULL,
                    ErrorsCount INTEGER NOT NULL,
                    SoftwareVersion TEXT NOT NULL,
                    SummaryNotes TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_ScanHistory_StartTime ON ScanHistory (StartTime DESC);
            ";

            using var cmd = new SqliteCommand(ddl, conn);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khởi tạo SQLite database lịch sử quét");
        }
    }

    public async Task<IReadOnlyList<ScanHistoryEntry>> GetAllAsync(int limit = 100)
    {
        await _dbLock.WaitAsync();
        try
        {
            var list = new List<ScanHistoryEntry>();
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string query = @"
                SELECT Id, ScanType, TargetPath, StartTime, EndTime, DurationMs, TotalFiles, TotalSizeBytes, ErrorsCount, SoftwareVersion, SummaryNotes
                FROM ScanHistory
                ORDER BY Id DESC
                LIMIT @Limit;
            ";

            await using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@Limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ScanHistoryEntry
                {
                    Id = reader.GetInt64(0),
                    ScanType = (ScanType)reader.GetInt32(1),
                    TargetPath = reader.GetString(2),
                    StartTime = DateTime.Parse(reader.GetString(3)),
                    EndTime = DateTime.Parse(reader.GetString(4)),
                    DurationMs = reader.GetInt64(5),
                    TotalFiles = reader.GetInt64(6),
                    TotalSizeBytes = reader.GetInt64(7),
                    ErrorsCount = reader.GetInt32(8),
                    SoftwareVersion = reader.GetString(9),
                    SummaryNotes = reader.IsDBNull(10) ? string.Empty : reader.GetString(10)
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi đọc lịch sử quét từ SQLite");
            return Array.Empty<ScanHistoryEntry>();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<ScanHistoryEntry?> GetByIdAsync(long id)
    {
        await _dbLock.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string query = @"
                SELECT Id, ScanType, TargetPath, StartTime, EndTime, DurationMs, TotalFiles, TotalSizeBytes, ErrorsCount, SoftwareVersion, SummaryNotes
                FROM ScanHistory
                WHERE Id = @Id;
            ";

            await using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ScanHistoryEntry
                {
                    Id = reader.GetInt64(0),
                    ScanType = (ScanType)reader.GetInt32(1),
                    TargetPath = reader.GetString(2),
                    StartTime = DateTime.Parse(reader.GetString(3)),
                    EndTime = DateTime.Parse(reader.GetString(4)),
                    DurationMs = reader.GetInt64(5),
                    TotalFiles = reader.GetInt64(6),
                    TotalSizeBytes = reader.GetInt64(7),
                    ErrorsCount = reader.GetInt32(8),
                    SoftwareVersion = reader.GetString(9),
                    SummaryNotes = reader.IsDBNull(10) ? string.Empty : reader.GetString(10)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi tìm lịch sử quét theo Id {Id}", id);
            return null;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task AddAsync(ScanHistoryEntry entry)
    {
        await _dbLock.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string query = @"
                INSERT INTO ScanHistory (ScanType, TargetPath, StartTime, EndTime, DurationMs, TotalFiles, TotalSizeBytes, ErrorsCount, SoftwareVersion, SummaryNotes)
                VALUES (@ScanType, @TargetPath, @StartTime, @EndTime, @DurationMs, @TotalFiles, @TotalSizeBytes, @ErrorsCount, @SoftwareVersion, @SummaryNotes);
                SELECT last_insert_rowid();
            ";

            await using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@ScanType", (int)entry.ScanType);
            cmd.Parameters.AddWithValue("@TargetPath", entry.TargetPath);
            cmd.Parameters.AddWithValue("@StartTime", entry.StartTime.ToString("o"));
            cmd.Parameters.AddWithValue("@EndTime", entry.EndTime.ToString("o"));
            cmd.Parameters.AddWithValue("@DurationMs", entry.DurationMs);
            cmd.Parameters.AddWithValue("@TotalFiles", entry.TotalFiles);
            cmd.Parameters.AddWithValue("@TotalSizeBytes", entry.TotalSizeBytes);
            cmd.Parameters.AddWithValue("@ErrorsCount", entry.ErrorsCount);
            cmd.Parameters.AddWithValue("@SoftwareVersion", entry.SoftwareVersion);
            cmd.Parameters.AddWithValue("@SummaryNotes", entry.SummaryNotes ?? string.Empty);

            var newId = await cmd.ExecuteScalarAsync();
            if (newId != null && long.TryParse(newId.ToString(), out long id))
            {
                entry.Id = id;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi ghi bản ghi lịch sử quét vào SQLite");
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task DeleteAsync(long id)
    {
        await _dbLock.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string query = "DELETE FROM ScanHistory WHERE Id = @Id;";
            await using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi xóa bản ghi lịch sử quét {Id}", id);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task ClearAllAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string query = "DELETE FROM ScanHistory;";
            await using var cmd = new SqliteCommand(query, conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi xóa toàn bộ lịch sử quét");
        }
        finally
        {
            _dbLock.Release();
        }
    }
}
