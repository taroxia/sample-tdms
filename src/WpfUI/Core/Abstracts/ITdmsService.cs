// ────────────────────────────────
//
// ────────────────────────────────

using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Core.Abstracts;


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

public record TdmsChannelInfo(
    string FilePath,
    string GroupName,
    string ChannelName,
    ulong SampleCount,
    DataType DataType = DataType.Double
);

public interface ITdmsService
{
    // メタデータのみ高速取得
    Task<IReadOnlyList<TdmsChannelInfo>> GetMetadataAsync(
        string filePath,
        CancellationToken ct = default);

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
