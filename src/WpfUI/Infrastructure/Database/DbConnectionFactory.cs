using System.Data.Common;
using DuckDB.NET.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using SqlKata.Compilers;

namespace WpfUI.Infrastructure.Database;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
    Compiler Compiler { get; }
}

// C# 13 Primary Constructor を使用
public sealed class DbConnectionFactory(IOptions<DatabaseOptions> options) : IDbConnectionFactory
{
    private readonly DatabaseOptions _config = options.Value;
    
    // アプリケーション起動時に一度だけ接続文字列を生成
    private readonly string _connectionString = options.Value.Provider switch
    {
        DatabaseProvider.PostgreSQL => new NpgsqlConnectionStringBuilder
        {
            Host = options.Value.Host,
            Port = options.Value.Port ?? 5432,
            Database = options.Value.DatabaseName,
            Username = options.Value.Username,
            Password = options.Value.Password,
            Pooling = true
        }.ConnectionString,

        DatabaseProvider.DuckDB => $"Data Source={options.Value.DuckDbFilePath}",
        _ => throw new NotSupportedException($"Provider {options.Value.Provider} is not supported.")
    };

    // コンパイラも一度だけ生成。DuckDBはPostgres互換として処理
    public Compiler Compiler { get; } = options.Value.Provider switch
    {
        DatabaseProvider.PostgreSQL or DatabaseProvider.DuckDB => new PostgresCompiler(),
        _ => throw new NotSupportedException()
    };

    public DbConnection CreateConnection()
    {
        // 呼び出しのたびに新しい Transient なコネクションを返す
        return _config.Provider switch
        {
            DatabaseProvider.PostgreSQL => new NpgsqlConnection(_connectionString),
            DatabaseProvider.DuckDB => new DuckDBConnection(_connectionString),
            _ => throw new NotSupportedException()
        };
    }
}
