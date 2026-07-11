using System;

namespace WinCarePro.Models;

public class NotificationItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Level { get; set; } = "Info"; // Info, Warning, Critical
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}
