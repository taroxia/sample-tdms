

# DuckDB

## URL
https://github.com/duckdb/duckdb/releases

1. DuckDB Releases にアクセスする。  
2. お使いのプラットフォーム（例：duckdb_cli-osx-universal.zip や duckdb_cli-windows-amd64.zip など）に合わせたファイルをダウンロード。
3. 解凍して得られる duckdb 実行ファイルを、システムの環境変数（PATH）に通したフォルダに配置する。

## バージョン確認
`duckdb --version`

## 

```
using Infrastructure.Database;
using SqlKata;

namespace Features.Measurements;

// C# 13 Primary Constructor
public sealed class GetActiveMeasurementsHandler(IDbConnectionFactory dbFactory)
{
    public async Task<IEnumerable<MeasurementDto>> HandleAsync()
    {
        // Feature層は「取得したいデータ」の定義のみに集中できる
        var query = new Query("nats.measurements")
            .Select("measurement_id", "title", "measured_at")
            .Where("status", "active")
            .OrderByDesc("measured_at")
            .Limit(100);

        // DatabaseExtensions による透過的な実行
        return await dbFactory.QueryAsync<MeasurementDto>(query);
    }
}
```