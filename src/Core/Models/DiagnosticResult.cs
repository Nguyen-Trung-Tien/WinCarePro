namespace WinCarePro.Models;

public class DiagnosticResult
{
    public string Category { get; set; } = "";
    public string CheckName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsHealthy { get; set; } = true;
    public string Details { get; set; } = "";
    public string Severity { get; set; } = "Low";
}

public class AiRecommendationItem
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ActionKey { get; set; } = "";
}

