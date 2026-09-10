using System.Text.Json.Serialization;

namespace WpfUI.Infrastructure.Persistence.Parquet;

/// <summary>
/// Parquetにシリアライズするための汎用時系列データモデル。
/// 1インスタンスが「1チャネル分の全波形データ」を表し、行として保存される。
/// Parquet.Net 6 のシリアライザは、配列を自動的にList構造としてシリアライズします。
/// </summary>
public sealed class SeriesRecord<TValue> where TValue : unmanaged
{
    // DBのメタデータと結合するための複合キー（LRUキャッシュキー等と同等）
    public string ContextKey { get; set; } = string.Empty;
    
    // チャネルごとの長さが異なる波形データ
    public TValue[] Data { get; set; } = [];
}
