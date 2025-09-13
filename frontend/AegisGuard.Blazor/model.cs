namespace AegisGuard.Blazor;

// Mutables Modell für Two-Way-Binding im Formular
public record SecurityLog
{
    public string source   { get; set; } = "";
    public string severity { get; set; } = "";
    public string message  { get; set; } = "";
    public string? metadata { get; set; }
    public DateTime? timestamp { get; set; }
}

public record LogStat
{
    public string severity { get; set; } = "";
    public int count { get; set; }
}
