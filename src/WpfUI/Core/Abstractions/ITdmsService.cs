// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using OpenTK.Audio.OpenAL;
using WpfUI.Core.Collections;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Core.Abstractions;


public enum DataType
{
    UInt8 = TdmNative.DataType.UInt8,
    Int16 = TdmNative.DataType.Int16,
    Int32 = TdmNative.DataType.Int32,
    Float = TdmNative.DataType.Float,
    Double = TdmNative.DataType.Double,
    String = TdmNative.DataType.String,
    Timestamp = TdmNative.DataType.Timestamp,
}
public record TdmsChannelMetadata(
    string Name,
    IReadOnlyDictionary<string, object> Properties,
    ulong SampleCount,
    DataType DataType = DataType.Double)
{
    // C# 13 field キーワード（セッター側での加工用：必要に応じて自動プロパティにも変更可）
    public string FilePath { get; init; }
    public string GroupName { get; init; }
    public TdmsCacheKey CacheKey { get; init; }
}

/// <summary>
/// グループメタデータ (不変データ構造)
/// </summary>
public record TdmsGroupMetadata(
    string Name,
    IReadOnlyDictionary<string, object> Properties,
    IReadOnlyList<TdmsChannelMetadata> Channels)
{
    public required string FilePath { get; init; }
}

/// <summary>
/// TDMSファイル全体のメタデータ構造 (スレッドセーフかつWPFバインディングに最適化)
/// </summary>
public sealed class TdmsFileMetadata
{
    public string FilePath { get; }
    public string Name { get; }
    public IReadOnlyList<TdmsGroupMetadata> Groups { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }

    // TdmsCacheKey をルックアップキーにすることで検索効率と安全性を最大化
    public IReadOnlyDictionary<TdmsCacheKey, TdmsChannelMetadata> ChannelLookup { get; }

    /// <summary>
    /// ファクトリメソッド経由でのみ生成を許可し、初期化の一貫性とスレッドセーフを保証
    /// </summary>
    private TdmsFileMetadata(
        string filePath,
        string name,
        IReadOnlyDictionary<string, object> properties,
        IReadOnlyList<TdmsGroupMetadata> groups,
        IReadOnlyDictionary<TdmsCacheKey, TdmsChannelMetadata> lookup)
    {
        FilePath = filePath;
        Name = name;
        Properties = properties;
        Groups = groups;
        ChannelLookup = lookup;
    }

    /// <summary>
    /// スレッドセーフに不変（Immutable）なメタデータツリーとルックアップ辞書を構築します。
    /// </summary>
    public static TdmsFileMetadata Create(
        string filePath,
        string name,
        IReadOnlyDictionary<string, object> rawProperties,
        IEnumerable<(string GroupName, IReadOnlyDictionary<string, object> Props, IEnumerable<TdmsChannelMetadata> Channels)> rawGroups)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var lookupBuilder = ImmutableDictionary.CreateBuilder<TdmsCacheKey, TdmsChannelMetadata>();
        var groupListBuilder = ImmutableList.CreateBuilder<TdmsGroupMetadata>();

        // 1パスで親から子へ不変プロパティを伝播させながら、完全な不変オブジェクトを生成
        foreach (var rawGroup in rawGroups)
        {
            var channelListBuilder = ImmutableList.CreateBuilder<TdmsChannelMetadata>();

            foreach (var rawChannel in rawGroup.Channels)
            {
                var cacheKey = new TdmsCacheKey(filePath, rawGroup.GroupName, rawChannel.Name);

                var frozenChannel = rawChannel with
                {
                    FilePath = filePath,
                    GroupName = rawGroup.GroupName,
                    CacheKey = cacheKey
                };

                channelListBuilder.Add(frozenChannel);
                lookupBuilder.Add(cacheKey, frozenChannel); // 一意性チェックを含む(重複があれば例外)
            }

            var frozenGroup = new TdmsGroupMetadata(rawGroup.GroupName, rawGroup.Props, channelListBuilder.ToImmutable())
            {
                FilePath = filePath
            };

            groupListBuilder.Add(frozenGroup);
        }

        return new TdmsFileMetadata(
            filePath,
            name,
            rawProperties.ToImmutableDictionary(), // プロパティもイミュータブル化
            groupListBuilder.ToImmutable(),
            lookupBuilder.ToImmutable()
        );
    }

    /// <summary>
    /// 逆引きを安全に行うためのTryGetメソッド
    /// </summary>
    public bool TryGetChannel(TdmsCacheKey key, [NotNullWhen(true)] out TdmsChannelMetadata? channel)
        => ChannelLookup.TryGetValue(key, out channel);
}

public interface ITdmsService
{
    // メタデータのみ高速取得
    Task<TdmsFileMetadata> GetMetadataAsync(
        string filepath,
        CancellationToken ct = default);
    //Task<IReadOnlyList<TdmsChannelMetadata>> GetMetadataAsync(
    //    string filePath,
    //    CancellationToken ct = default);

    // ストリーミング読み込み（巨大データ対応）
    //IAsyncEnumerable<double> ReadChannelDataAsync(
    //    string filePath,
    //    string group,
    //    string channel,
    //    int bufferSize = 100_000,
    //    CancellationToken ct = default);
    IAsyncEnumerable<double> ReadChannelDataStreamAsync(
        string filePath,
        string group,
        string channel,
        nuint chunkSize = 10000,
        CancellationToken ct = default);
    /*
    // Downsampling（描画向け）
    Task<double[]> ReadResampledDataAsync(
        string filePath,
        string group,
        string channel,
        int targetCount,
        CancellationToken ct = default);
    */

    // ファイル以外の入力にも対応（商用では必須）
    //Task<IReadOnlyList<TdmsChannelInfo>> GetMetadataAsync(
    //    Stream stream,
    //    CancellationToken ct = default);
    //IAsyncEnumerable<double> ReadChannelDataAsync(
    //    Stream stream,
    //    string group,
    //    string channel,
    //    int bufferSize = 100_000,
    //    CancellationToken ct = default);
    //Task<double[]> ReadResampledDataAsync(
    //    Stream stream,
    //    string group,
    //    string channel,
    //    int targetCount,
    //    CancellationToken ct = default);
}
