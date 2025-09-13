namespace AegisGuard.Domain;

public class SecurityLog
{
    public int Id { get; set; }               // PK
    public string Source { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Metadata { get; set; }
    public DateTime Timestamp { get; set; }
}
