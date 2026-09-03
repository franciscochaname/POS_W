namespace POS_W.Data;

public sealed record PosDatabaseSettings(string? ConnectionString)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
