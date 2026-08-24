using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;
using WinCarePro.Models;

namespace WinCarePro.Database;

public class DbManager
{
    private static readonly object DbLock = new();

    public static event Action<WinCarePro.Models.NotificationItem>? OnNotificationAdded;
    public static event Action<LogEntry>? OnLogAdded;

    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "WinCarePro"
    );
    private static readonly string DbPath = Path.Combine(AppDataPath, "wincaredb.db");
    private static readonly string ConnectionString = $"Data Source={DbPath};";

    private static SqliteConnection CreateAndOpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        try
        {
            connection.Open();
            using (var cmd = new SqliteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000; PRAGMA cache_size=-2000; PRAGMA temp_store=MEMORY;", connection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch { }
            }
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static T ExecuteWithConnection<T>(Func<SqliteConnection, T> operation, T defaultValue = default!)
    {
        lock (DbLock)
        {
            int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var connection = CreateAndOpenConnection();
                    return operation(connection);
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5 || ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("busy", StringComparison.OrdinalIgnoreCase))
                {
                    if (attempt == maxRetries - 1) return defaultValue;
                    Thread.Sleep(50 * (attempt + 1));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    public static void ExecuteInTransaction(Action<SqliteConnection, SqliteTransaction> operation)
    {
        ExecuteWithConnection(connection =>
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                operation(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        });
    }

    private static void ExecuteWithConnection(Action<SqliteConnection> operation)
    {
        lock (DbLock)
        {
            int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var connection = CreateAndOpenConnection();
                    operation(connection);
                    return;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5 || ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("busy", StringComparison.OrdinalIgnoreCase))
                {
                    if (attempt == maxRetries - 1) return;
                    Thread.Sleep(50 * (attempt + 1));
                }
                catch
                {
                    return;
                }
            }
        }
    }

    private static volatile string? _cachedSettings;

    public static void InitializeDatabase()
    {
        _cachedSettings = null; // Clear cache on database initialization
        if (!Directory.Exists(AppDataPath))
        {
            try
            {
                Directory.CreateDirectory(AppDataPath);
            }
            catch { }
        }

        ExecuteWithConnection(connection =>
        {
            // Create Users table
            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL,
                    Settings TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            using (var command = new SqliteCommand(createUsersTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create Logs table
            var createLogsTable = @"
                CREATE TABLE IF NOT EXISTS Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Action TEXT NOT NULL,
                    Module TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            using (var command = new SqliteCommand(createLogsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create index on Logs (Module, CreatedAt)
            var createLogsIndex = "CREATE INDEX IF NOT EXISTS idx_logs_module_createdat ON Logs (Module, CreatedAt DESC);";
            using (var command = new SqliteCommand(createLogsIndex, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create Reports table
            var createReportsTable = @"
                CREATE TABLE IF NOT EXISTS Reports (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReportName TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            using (var command = new SqliteCommand(createReportsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create UpdatedApps table
            var createUpdatedAppsTable = @"
                CREATE TABLE IF NOT EXISTS UpdatedApps (
                    AppId TEXT PRIMARY KEY,
                    Version TEXT NOT NULL,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            using (var command = new SqliteCommand(createUpdatedAppsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create Notifications table
            var createNotificationsTable = @"
                CREATE TABLE IF NOT EXISTS Notifications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    Level TEXT NOT NULL,
                    IsRead INTEGER DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            using (var command = new SqliteCommand(createNotificationsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create StateSnapshots table for Undo Rollback
            var createSnapshotsTable = @"
                CREATE TABLE IF NOT EXISTS StateSnapshots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Category TEXT NOT NULL,
                    KeyName TEXT NOT NULL,
                    OriginalValue TEXT,
                    NewValue TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            using (var command = new SqliteCommand(createSnapshotsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create index on Notifications (IsRead, CreatedAt)
            var createNotificationsIndex = "CREATE INDEX IF NOT EXISTS idx_notifications_isread_createdat ON Notifications (IsRead, CreatedAt DESC);";
            using (var command = new SqliteCommand(createNotificationsIndex, connection))
            {
                command.ExecuteNonQuery();
            }

            // Check if default user exists, if not, create one
            var checkUser = "SELECT COUNT(*) FROM Users";
            long userCount = 0;
            using (var command = new SqliteCommand(checkUser, connection))
            {
                userCount = (long)(command.ExecuteScalar() ?? 0L);
            }

            if (userCount == 0)
            {
                var insertUser = "INSERT INTO Users (Id, Username, Settings) VALUES (1, $username, $settings) ON CONFLICT(Id) DO NOTHING;";
                using var command = new SqliteCommand(insertUser, connection);
                command.Parameters.AddWithValue("$username", Environment.UserName);
                command.Parameters.AddWithValue("$settings", "{\"Theme\":\"Dark\",\"AutoScan\":false,\"ReportFormat\":\"PDF\"}");
                command.ExecuteNonQuery();
            }
        });

        // Warmup: Pre-populate settings cache to avoid first cold DB hit on startup
        GetSettings();
    }

    public static void LogAction(string action, string module, string status)
    {
        ExecuteWithConnection(connection =>
        {
            var insertLog = "INSERT INTO Logs (Action, Module, Status) VALUES ($action, $module, $status)";
            using var command = new SqliteCommand(insertLog, connection);
            command.Parameters.AddWithValue("$action", action);
            command.Parameters.AddWithValue("$module", module);
            command.Parameters.AddWithValue("$status", status);
            command.ExecuteNonQuery();
        });

        try
        {
            OnLogAdded?.Invoke(new LogEntry
            {
                Action = action,
                Module = module,
                Status = status,
                CreatedAt = DateTime.Now
            });
        }
        catch { }
    }

    public static List<LogEntry> GetLogs(string? module = null, string? search = null)
    {
        return ExecuteWithConnection(connection =>
        {
            var logs = new List<LogEntry>();
            var query = "SELECT Id, Action, Module, Status, CreatedAt FROM Logs";
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(module))
            {
                conditions.Add("Module = $module");
            }
            if (!string.IsNullOrEmpty(search))
            {
                conditions.Add("(Action LIKE $search OR Module LIKE $search OR Status LIKE $search)");
            }

            if (conditions.Count > 0)
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }
            query += " ORDER BY CreatedAt DESC LIMIT 200";

            using var command = new SqliteCommand(query, connection);
            if (!string.IsNullOrEmpty(module))
            {
                command.Parameters.AddWithValue("$module", module);
            }
            if (!string.IsNullOrEmpty(search))
            {
                command.Parameters.AddWithValue("$search", $"%{search}%");
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new LogEntry
                {
                    Id = reader.GetInt32(0),
                    Action = reader.GetString(1),
                    Module = reader.GetString(2),
                    Status = reader.GetString(3),
                    CreatedAt = DateTime.TryParse(reader.GetValue(4)?.ToString(), out var dt) ? dt : DateTime.Now
                });
            }
            return logs;
        }, new List<LogEntry>());
    }

    public static void SaveReport(string name, string filePath)
    {
        ExecuteWithConnection(connection =>
        {
            var insertReport = "INSERT INTO Reports (ReportName, FilePath) VALUES ($name, $filePath)";
            using var command = new SqliteCommand(insertReport, connection);
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$filePath", filePath);
            command.ExecuteNonQuery();
        });
    }

    public static List<ReportEntry> GetReports()
    {
        return ExecuteWithConnection(connection =>
        {
            var reports = new List<ReportEntry>();
            var query = "SELECT Id, ReportName, FilePath, CreatedAt FROM Reports ORDER BY CreatedAt DESC";
            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                reports.Add(new ReportEntry
                {
                    Id = reader.GetInt32(0),
                    ReportName = reader.GetString(1),
                    FilePath = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3)
                });
            }
            return reports;
        }, new List<ReportEntry>());
    }

    public static string GetSettings()
    {
        // Thread-safe cache read: capture volatile reference once
        var cached = _cachedSettings;
        if (cached != null) return cached;

        return ExecuteWithConnection(connection =>
        {
            var query = "SELECT Settings FROM Users ORDER BY Id ASC LIMIT 1";
            using var command = new SqliteCommand(query, connection);
            var result = command.ExecuteScalar();
            var value = result?.ToString() ?? "";
            _cachedSettings = value; // Atomic volatile write
            return value;
        }, "");
    }

    public static void SaveSettings(string settings)
    {
        // Validate JSON structure before persisting — prevents corrupt settings from crashing all callers
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(settings);
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("[DbManager] SaveSettings rejected: invalid JSON");
            return; // Reject invalid JSON silently
        }

        _cachedSettings = settings;
        ExecuteWithConnection(connection =>
        {
            var updateSettings = "INSERT INTO Users (Id, Username, Settings) VALUES (1, 'DefaultUser', $settings) ON CONFLICT(Id) DO UPDATE SET Settings = $settings;";
            using var command = new SqliteCommand(updateSettings, connection);
            command.Parameters.AddWithValue("$settings", settings);
            command.ExecuteNonQuery();
        });
    }

    public static void SaveUpdatedApp(string appId, string version)
    {
        ExecuteWithConnection(connection =>
        {
            var query = "INSERT OR REPLACE INTO UpdatedApps (AppId, Version) VALUES ($appId, $version)";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("$appId", appId);
            command.Parameters.AddWithValue("$version", version);
            command.ExecuteNonQuery();
        });
    }

    public static Dictionary<string, string> GetUpdatedApps()
    {
        return ExecuteWithConnection(connection =>
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var query = "SELECT AppId, Version FROM UpdatedApps";
            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dict[reader.GetString(0)] = reader.GetString(1);
            }
            return dict;
        }, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public static int CleanupOldLogs(int retentionDays = 90)
    {
        return ExecuteWithConnection(connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Logs WHERE CreatedAt < @cutoff";
            cmd.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-retentionDays).ToString("o"));
            return cmd.ExecuteNonQuery();
        }, 0);
    }

    public static List<LogEntry> GetRecentLogs(int limit = 50)
    {
        return ExecuteWithConnection(connection =>
        {
            var logs = new List<LogEntry>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Action, Module, Status, CreatedAt FROM Logs ORDER BY Id DESC LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new LogEntry
                {
                    Id = reader.GetInt32(0),
                    Action = reader.GetString(1),
                    Module = reader.GetString(2),
                    Status = reader.GetString(3),
                    CreatedAt = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.Now
                });
            }
            return logs;
        }, new List<LogEntry>());
    }

    public static void AddNotification(string title, string message, string level = "Info", bool showToast = true)
    {
        ExecuteWithConnection(connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Notifications (Title, Message, Level, IsRead) VALUES (@title, @message, @level, 0)";
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Parameters.AddWithValue("@level", level);
            cmd.ExecuteNonQuery();
        });

        try
        {
            OnNotificationAdded?.Invoke(new WinCarePro.Models.NotificationItem
            {
                Title = title,
                Message = message,
                Level = level,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
        }
        catch { }

        var win = WinCarePro.App.MainWindowInstance;
        if (win != null)
        {
            win.UpdateNotificationBadge();
            if (showToast)
            {
                win.ShowToastFromDb(title, message, level);
            }
        }
    }

    public static List<WinCarePro.Models.NotificationItem> GetRecentNotifications(int limit = 50)
    {
        return ExecuteWithConnection(connection =>
        {
            var list = new List<WinCarePro.Models.NotificationItem>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Message, Level, IsRead, CreatedAt FROM Notifications ORDER BY Id DESC LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WinCarePro.Models.NotificationItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Message = reader.GetString(2),
                    Level = reader.GetString(3),
                    IsRead = reader.GetInt32(4) != 0,
                    CreatedAt = DateTime.TryParse(reader.GetValue(5)?.ToString(), out var dt) ? dt : DateTime.Now
                });
            }
            return list;
        }, new List<WinCarePro.Models.NotificationItem>());
    }

    public static void MarkAllNotificationsAsRead()
    {
        ExecuteWithConnection(connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Notifications SET IsRead = 1 WHERE IsRead = 0";
            cmd.ExecuteNonQuery();

            WinCarePro.App.MainWindowInstance?.UpdateNotificationBadge();
        });
    }

    public static int GetUnreadNotificationsCount()
    {
        return ExecuteWithConnection(connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Notifications WHERE IsRead = 0";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }, 0);
    }

    public static void ClearAllNotifications()
    {
        ExecuteWithConnection(connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Notifications";
            cmd.ExecuteNonQuery();

            WinCarePro.App.MainWindowInstance?.UpdateNotificationBadge();
        });
    }

    public static void DeleteNotification(int id)
    {
        ExecuteWithConnection(connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Notifications WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            WinCarePro.App.MainWindowInstance?.UpdateNotificationBadge();
        });
    }

    public static void RunDatabaseMaintenance()
    {
        // Check if database maintenance was run in the last 7 days to avoid heavy VACUUM on every launch
        bool shouldRun = false;
        try
        {
            var logs = GetLogs("Database", "Database maintenance completed.");
            if (logs.Count == 0)
            {
                shouldRun = true;
            }
            else
            {
                var lastRun = logs[0].CreatedAt;
                if ((DateTime.Now - lastRun).TotalDays >= 7)
                {
                    shouldRun = true;
                }
            }
        }
        catch
        {
            shouldRun = true; // Safe fallback
        }

        if (!shouldRun) return;

        ExecuteWithConnection(connection =>
        {
            int retentionDays = 30; // Default fallback
            try
            {
                string raw = GetSettings();
                if (!string.IsNullOrEmpty(raw))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("PerformanceHistoryDurationIndex", out var durationProp))
                    {
                        int index = durationProp.GetInt32();
                        retentionDays = index switch
                        {
                            0 => 7,   // 7 Days
                            1 => 30,  // 30 Days
                            2 => 90,  // 90 Days
                            _ => 30
                        };
                    }
                }
            }
            catch { }

            // 1. Auto clean logs older than retentionDays days before compaction
            try
            {
                using var cleanCmd = connection.CreateCommand();
                cleanCmd.CommandText = "DELETE FROM Logs WHERE CreatedAt < @cutoff";
                cleanCmd.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-retentionDays).ToString("yyyy-MM-dd HH:mm:ss"));
                cleanCmd.ExecuteNonQuery();
            }
            catch { }

            // 2. Auto clean state snapshots older than 30 days
            try
            {
                using var cleanSnapCmd = connection.CreateCommand();
                cleanSnapCmd.CommandText = "DELETE FROM StateSnapshots WHERE CreatedAt < @cutoff";
                cleanSnapCmd.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss"));
                cleanSnapCmd.ExecuteNonQuery();
            }
            catch { }

            // 3. Reclaim freed disk space, defragment tables, and truncate WAL logs
            try
            {
                using var cmd = new SqliteCommand("PRAGMA optimize; VACUUM; ANALYZE; PRAGMA wal_checkpoint(TRUNCATE);", connection);
                cmd.ExecuteNonQuery();
            }
            catch { }
        });

        // Log the completion of database maintenance to schedule the next run in 7 days
        LogAction("Database maintenance completed.", "Database", "Success");
    }

    #region DPAPI Security & Snapshot Methods

    public static string EncryptProtectedData(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        try
        {
            return Infrastructure.Security.CryptoHelper.ProtectString(plainText);
        }
        catch (Exception ex)
        {
            // Never expose plaintext on encryption failure — log and return empty
            Infrastructure.Logging.CrashLogger.LogException("DPAPI_ENCRYPT", ex);
            return string.Empty;
        }
    }

    public static string DecryptProtectedData(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        try
        {
            return Infrastructure.Security.CryptoHelper.UnprotectString(cipherText);
        }
        catch (Exception ex)
        {
            // Never expose ciphertext as plaintext fallback — log and return empty
            Infrastructure.Logging.CrashLogger.LogException("DPAPI_DECRYPT", ex);
            return string.Empty;
        }
    }

    public static void SaveSnapshot(string category, string keyName, string? originalValue, string? newValue)
    {
        ExecuteWithConnection(connection =>
        {
            var sql = "INSERT INTO StateSnapshots (Category, KeyName, OriginalValue, NewValue) VALUES ($cat, $key, $orig, $new)";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("$cat", category);
            cmd.Parameters.AddWithValue("$key", keyName);
            cmd.Parameters.AddWithValue("$orig", (object?)originalValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$new", (object?)newValue ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        });
    }

    public static List<StateSnapshotEntry> GetSnapshots(string? category = null)
    {
        return ExecuteWithConnection(connection =>
        {
            var list = new List<StateSnapshotEntry>();
            var sql = "SELECT Id, Category, KeyName, OriginalValue, NewValue, CreatedAt FROM StateSnapshots";
            if (!string.IsNullOrEmpty(category))
            {
                sql += " WHERE Category = $cat";
            }
            sql += " ORDER BY CreatedAt DESC LIMIT 100";

            using var cmd = new SqliteCommand(sql, connection);
            if (!string.IsNullOrEmpty(category))
            {
                cmd.Parameters.AddWithValue("$cat", category);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new StateSnapshotEntry
                {
                    Id = reader.GetInt32(0),
                    Category = reader.GetString(1),
                    KeyName = reader.GetString(2),
                    OriginalValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                    NewValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CreatedAt = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5)
                });
            }
            return list;
        }, new List<StateSnapshotEntry>());
    }

    #region Database Lifecycle & Graceful Shutdown

    public static void ShutdownDatabase()
    {
        lock (DbLock)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                using var cmd = new SqliteCommand("PRAGMA wal_checkpoint(TRUNCATE);", connection);
                cmd.ExecuteNonQuery();
            }
            catch { }
            finally
            {
                try
                {
                    SqliteConnection.ClearAllPools();
                }
                catch { }
            }
        }
    }

    #endregion

    #endregion
}

public class StateSnapshotEntry
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string KeyName { get; set; } = "";
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LogEntry
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string Module { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public string DisplayAction => Services.TranslationManager.Instance.T(Action);
    public string DisplayModule => Services.TranslationManager.Instance.T(Module);
    public string DisplayStatus => Services.TranslationManager.Instance.T(Status);

    public Microsoft.UI.Xaml.Media.Brush StatusBrush => GetStatusBrush(Status);
    public Microsoft.UI.Xaml.Media.Brush StatusTintBrush => GetStatusTintBrush(Status);
    public string StatusGlyph => GetStatusGlyph(Status);

    public string RelativeTimeAgo
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;
            if (diff.TotalMinutes < 1) return "Just Now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return CreatedAt.ToString("MMM dd");
        }
    }

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush SuccessBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush WarningBrush = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush ErrorBrush = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush InfoBrush = new(Windows.UI.Color.FromArgb(255, 139, 92, 246));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush SuccessTint = new(Windows.UI.Color.FromArgb(38, 16, 185, 129));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush WarningTint = new(Windows.UI.Color.FromArgb(38, 245, 158, 11));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush ErrorTint = new(Windows.UI.Color.FromArgb(38, 239, 68, 68));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush InfoTint = new(Windows.UI.Color.FromArgb(38, 139, 92, 246));

    public static Microsoft.UI.Xaml.Media.SolidColorBrush GetStatusBrush(string? status)
    {
        if (string.IsNullOrEmpty(status)) return InfoBrush;
        string lower = status.ToLower();
        if (lower.Contains("fail") || lower.Contains("err") || lower.Contains("crit"))
            return ErrorBrush;
        if (lower.Contains("warn") || lower.Contains("skip"))
            return WarningBrush;
        if (lower.Contains("succ") || lower.Contains("ok") || lower.Contains("done") || lower.Contains("clean"))
            return SuccessBrush;
        return InfoBrush;
    }

    public static Microsoft.UI.Xaml.Media.SolidColorBrush GetStatusTintBrush(string? status)
    {
        if (string.IsNullOrEmpty(status)) return InfoTint;
        string lower = status.ToLower();
        if (lower.Contains("fail") || lower.Contains("err") || lower.Contains("crit"))
            return ErrorTint;
        if (lower.Contains("warn") || lower.Contains("skip"))
            return WarningTint;
        if (lower.Contains("succ") || lower.Contains("ok") || lower.Contains("done") || lower.Contains("clean"))
            return SuccessTint;
        return InfoTint;
    }

    public static string GetStatusGlyph(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "\uE946";
        string lower = status.ToLower();
        if (lower.Contains("fail") || lower.Contains("err") || lower.Contains("crit"))
            return "\uEA39";
        if (lower.Contains("warn") || lower.Contains("skip"))
            return "\uE7BA";
        if (lower.Contains("succ") || lower.Contains("ok") || lower.Contains("done") || lower.Contains("clean"))
            return "\uE73E";
        return "\uE946";
    }
}

public class ReportEntry
{
    public int Id { get; set; }
    public string ReportName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
