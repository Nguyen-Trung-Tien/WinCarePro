using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace WinCarePro.Database;

public class DbManager
{
    private static readonly SemaphoreSlim DbSemaphore = new(1, 1);

    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "WinCarePro"
    );
    private static readonly string DbPath = Path.Combine(AppDataPath, "wincaredb.db");
    private static readonly string ConnectionString = $"Data Source={DbPath};";

    private static SqliteConnection CreateAndOpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using (var cmd = new SqliteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;", connection))
        {
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch { }
        }
        return connection;
    }

    private static T ExecuteWithConnection<T>(Func<SqliteConnection, T> operation, T defaultValue = default!)
    {
        DbSemaphore.Wait();
        try
        {
            using var connection = CreateAndOpenConnection();
            return operation(connection);
        }
        catch
        {
            return defaultValue;
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    private static void ExecuteWithConnection(Action<SqliteConnection> operation)
    {
        DbSemaphore.Wait();
        try
        {
            using var connection = CreateAndOpenConnection();
            operation(connection);
        }
        catch
        {
            // Fail silently
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static void InitializeDatabase()
    {
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

            // Check if default user exists, if not, create one
            var checkUser = "SELECT COUNT(*) FROM Users";
            long userCount = 0;
            using (var command = new SqliteCommand(checkUser, connection))
            {
                userCount = (long)(command.ExecuteScalar() ?? 0L);
            }

            if (userCount == 0)
            {
                var insertUser = "INSERT INTO Users (Username, Settings) VALUES ($username, $settings)";
                using var command = new SqliteCommand(insertUser, connection);
                command.Parameters.AddWithValue("$username", Environment.UserName);
                command.Parameters.AddWithValue("$settings", "{\"Theme\":\"Dark\",\"AutoScan\":false,\"ReportFormat\":\"PDF\"}");
                command.ExecuteNonQuery();
            }
        });
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
                    CreatedAt = reader.GetDateTime(4)
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
        return ExecuteWithConnection(connection =>
        {
            var query = "SELECT Settings FROM Users LIMIT 1";
            using var command = new SqliteCommand(query, connection);
            var result = command.ExecuteScalar();
            return result?.ToString() ?? "";
        }, "");
    }

    public static void SaveSettings(string settings)
    {
        ExecuteWithConnection(connection =>
        {
            var updateSettings = "UPDATE Users SET Settings = $settings";
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

            var win = WinCarePro.App.MainWindowInstance;
            if (win != null)
            {
                win.UpdateNotificationBadge();
                if (showToast)
                {
                    win.ShowToastFromDb(title, message, level);
                }
            }
        });
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
                    CreatedAt = DateTime.TryParse(reader.GetString(5), out var dt) ? dt : DateTime.Now
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
        ExecuteWithConnection(connection =>
        {
            using var cmd = new SqliteCommand("VACUUM; ANALYZE;", connection);
            cmd.ExecuteNonQuery();

            // Auto clean logs older than 30 days during weekly maintenance
            using var cleanCmd = connection.CreateCommand();
            cleanCmd.CommandText = "DELETE FROM Logs WHERE CreatedAt < @cutoff";
            cleanCmd.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-30).ToString("o"));
            cleanCmd.ExecuteNonQuery();
        });
    }
}

public class LogEntry
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string Module { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}

public class ReportEntry
{
    public int Id { get; set; }
    public string ReportName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
