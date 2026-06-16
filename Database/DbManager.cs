using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WinCarePro.Database;

public class DbManager
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "WinCarePro"
    );
    private static readonly string DbPath = Path.Combine(AppDataPath, "wincaredb.db");
    private static readonly string ConnectionString = $"Data Source={DbPath}";

    public static void InitializeDatabase()
    {
        if (!Directory.Exists(AppDataPath))
        {
            Directory.CreateDirectory(AppDataPath);
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

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
    }

    public static void LogAction(string action, string module, string status)
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            var insertLog = "INSERT INTO Logs (Action, Module, Status) VALUES ($action, $module, $status)";
            using var command = new SqliteCommand(insertLog, connection);
            command.Parameters.AddWithValue("$action", action);
            command.Parameters.AddWithValue("$module", module);
            command.Parameters.AddWithValue("$status", status);
            command.ExecuteNonQuery();
        }
        catch
        {
            // Fail silently or log to diagnostic trace in a real app
        }
    }

    public static List<LogEntry> GetLogs(string? module = null, string? search = null)
    {
        var logs = new List<LogEntry>();
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
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
        }
        catch
        {
            // Return empty list
        }
        return logs;
    }

    public static void SaveReport(string name, string filePath)
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            var insertReport = "INSERT INTO Reports (ReportName, FilePath) VALUES ($name, $filePath)";
            using var command = new SqliteCommand(insertReport, connection);
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$filePath", filePath);
            command.ExecuteNonQuery();
        }
        catch
        {
            // Fail silently
        }
    }

    public static List<ReportEntry> GetReports()
    {
        var reports = new List<ReportEntry>();
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
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
        }
        catch
        {
            // Return empty
        }
        return reports;
    }

    public static string GetSettings()
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            var query = "SELECT Settings FROM Users LIMIT 1";
            using var command = new SqliteCommand(query, connection);
            var result = command.ExecuteScalar();
            return result?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    public static void SaveSettings(string settings)
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            var updateSettings = "UPDATE Users SET Settings = $settings";
            using var command = new SqliteCommand(updateSettings, connection);
            command.Parameters.AddWithValue("$settings", settings);
            command.ExecuteNonQuery();
        }
        catch
        {
            // Fail silently
        }
    }
}

public class LogEntry
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string Module { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ReportEntry
{
    public int Id { get; set; }
    public string ReportName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
