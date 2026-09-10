using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WpfUI.Infrastructure.Persistence.Parquet;

namespace WpfUI.Application.Services;

/// <summary>
/// 波形データのParquet入出力をオーケストレーションするサービス。
/// C# 13 のプライマリコンストラクタを使用し、DIをスッキリ記述。
/// </summary>
public sealed class WaveformDataService(IParquetStorageService parquetService)
{
    /// <summary>
    /// 【Write】テスト終了時に複数チャネルのデータを一括保存する
    /// </summary>
    public async Task ExportTestSessionAsync(string filePath, CancellationToken ct = default)
    {
        // ダミーデータの生成 (実際の要件では計測デバイス等から取得)
        // コレクション式 [] を活用してスッキリと初期化
        List<SeriesRecord<double>> records =
        [
            new() { ContextKey = "Session_001|Group_A|Ch_01", Data = GenerateSineWave(1000) },
            new() { ContextKey = "Session_001|Group_A|Ch_02", Data = GenerateSineWave(500) }, // 長さが異なってもOK
            new() { ContextKey = "Session_001|Group_B|Ch_01", Data = GenerateSineWave(2000) }
        ];

        // ParquetStorageServiceへの委譲。非同期処理の待機とキャンセルトークンの伝播を徹底。
        await parquetService.WriteRecordsAsync(filePath, records, ct);
    }

    /// <summary>
    /// 【Read】画面描画時などに、DBから指定されたキーの波形データ「のみ」を抽出する
    /// </summary>
    public async Task<double[]> GetChannelWaveformAsync(
        string filePath, 
        string targetKey, 
        CancellationToken ct = default)
    {
        // System.Linq.Async の FirstOrDefaultAsync を使用。
        // ReadRecordsAsync が返す IAsyncEnumerable<T> に対し、LINQで宣言的にフィルタリングを行う。
        var targetRecord = await parquetService
            .ReadRecordsAsync<SeriesRecord<double>>(filePath, ct)
            .FirstOrDefaultAsync(record => record.ContextKey == targetKey, ct);

        // レコードが見つからなければ空配列を返す (nullを返さず安全にフォールバック)
        return targetRecord?.Data ?? [];
    }

    // ダミー波形生成用ヘルパー
    private static double[] GenerateSineWave(int samples)
    {
        var data = new double[samples];
        for (int i = 0; i < samples; i++)
        {
            data[i] = Math.Sin(i * 0.1);
        }
        return data;
    }
}
