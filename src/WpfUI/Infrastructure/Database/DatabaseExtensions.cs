// ────────────────────────────────
//
// ────────────────────────────────

using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlKata;

namespace WpfUI.Infrastructure.Database;

public static class DatabaseExtensions
{
    /// <summary>
    /// DIコンテナへのインフラストラクチャ登録
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        return services;
    }

    /// <summary>
    /// DapperとSqlKataを透過的に結合する最強の拡張メソッド
    /// </summary>
    public static async Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnectionFactory factory,
        Query query)
    {
        // コンパイル
        var compiled = factory.Compiler.Compile(query);

        // コネクションの自動破棄とDapper実行を1ステートメントにカプセル化
        await using var connection = factory.CreateConnection();
        return await connection.QueryAsync<T>(compiled.Sql, compiled.NamedBindings);
    }

    public static async Task<int> ExecuteAsync(
        this IDbConnectionFactory factory,
        Query query)
    {
        var compiled = factory.Compiler.Compile(query);
        await using var connection = factory.CreateConnection();
        return await connection.ExecuteAsync(compiled.Sql, compiled.NamedBindings);
    }
}
