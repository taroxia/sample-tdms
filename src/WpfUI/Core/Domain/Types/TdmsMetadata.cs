// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfUI.Core.Collections;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Core.Domain.Types;

/// <summary>
/// 
/// </summary>
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

/// <summary>
/// 
/// </summary>
public record TdmsChannelMetadata(
    string Name,
    IReadOnlyDictionary<string, object> Properties,
    ulong SampleCount,
    DataType DataType = DataType.Double)
{
    public string FilePath { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public TdmsCacheKey CacheKey { get; init; }

    public override int GetHashCode() => CacheKey.GetHashCode();
}

/// <summary>
/// 
/// </summary>
public record TdmsGroupMetadata(
    string Name,
    IReadOnlyDictionary<string, object> Properties,
    IReadOnlyList<TdmsChannelMetadata> Channels)
{
    public required string FilePath { get; init; }
}

/// <summary>
/// 
/// </summary>
public sealed class TdmsFileMetadata
{
    public string FilePath { get; } = string.Empty;
    public string Name { get; } = string.Empty;
    public IReadOnlyList<TdmsGroupMetadata> Groups { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }

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
            rawProperties.ToImmutableDictionary(),
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
