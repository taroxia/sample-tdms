namespace WpfUI.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public required DatabaseProvider Provider { get; init; }= DatabaseProvider.DuckDB;
    
    // PostgreSQL
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? DatabaseName { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }

    // DuckDB
    public string? DuckDbFilePath { get; init; } = "app_data.duckdb";
}

public enum DatabaseProvider
{
    PostgreSQL,
    DuckDB
}
