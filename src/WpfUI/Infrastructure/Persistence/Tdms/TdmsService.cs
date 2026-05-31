// ────────────────────────────────
//
// ────────────────────────────────

using System.Runtime.CompilerServices;
using System.Xml.Linq;
using OpenTK.Audio.OpenAL;
using WpfUI.Core.Abstractions;
using WpfUI.Core.Collections;
using WpfUI.Core.Dmain.Models;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Infrastructure.Persistence.Tdms;

public sealed class TdmsService : ITdmsService
{
    public async Task<TdmsFileMetadata> GetMetadataAsync(string filepath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await Task.Run(() =>
        {
            using var wrapper = new TdmsWrapper(filepath);
            wrapper.OpenFile();

            IReadOnlyDictionary<string, object> ValueToObject(IEnumerable<KeyValuePair<string, TdmsValue>> rawProps)
            {
                return rawProps
                    .Select(kv => (
                        kv.Key,
                        Value: kv.Value switch
                        {
                            TdmsValue.UInt8(var v) => (object)v,
                            TdmsValue.Int16(var v) => v,
                            TdmsValue.Int32(var v) => v,
                            TdmsValue.Float(var v) => v,
                            TdmsValue.Double(var v) => v,
                            TdmsValue.String(var v) => v,
                            TdmsValue.Timestamp(var v) => v,
                            _ => null
                        }))
                    .Where(t => t.Value is not null)
                    .ToDictionary(t => t.Key, t => t.Value!);
            }

            var rawGroupData = wrapper.GetChannelGroups().Select(gHandle =>
            {
                // --- Channel Prop.
                var rawChannels = wrapper.GetChannels(gHandle).Select(cHandle => new TdmsChannelMetadata(
                    Name: wrapper.GetChannelName(cHandle),
                    Properties: ValueToObject(wrapper.GetProperties(cHandle)),
                    SampleCount: wrapper.GetNumDataValues(cHandle),
                    DataType: (DataType)wrapper.GetDataType(cHandle)
                ));

                // --- Group Prop.
                return (
                    GroupName: wrapper.GetChannelGroupName(gHandle),
                    Props: ValueToObject(wrapper.GetProperties(gHandle)),
                    Channels: rawChannels
                );
            }).ToList();

            ct.ThrowIfCancellationRequested();

            // --- File Prop.
            return TdmsFileMetadata.Create(
                filePath: filepath,
                name: wrapper.GetFileName(),
                rawProperties: ValueToObject(wrapper.GetProperties()),
                rawGroups: rawGroupData
            );
        }, ct);
    }

    /*
    // ScottPlot用：全データ取得（バルクリード）
    public async Task<double[]> ReadChannelDataAsync(string filePath, string group, string channel, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var file = new TdmsHandler(filePath);
            if (!TryGetChannelHandle(file.Handle, group, channel, out int chHandle))
                return [];

            TdmNative.DDC_GetNumDataValues(chHandle, out long count);
            var data = new double[count];
            
            // NI DLL の DDC_GetDataValues はスレッドセーフではないため、Task.Run 内で完結させる
            int status = TdmNative.DDC_GetDataValues(chHandle, 0, count, data);
            return status == 0 ? data : [];
        }, ct);
    }
    */

    public async ValueTask<TdmsData> ReadChannelDataStreamAsync(
        TdmsCacheKey key,
        CancellationToken cancellationToken = default)
    {
        // 大容量ファイルのI/Oを伴うため、WPFのUIスレッドをブロックしないよう、
        // ThreadPool（Task.Run）へ明示的に逃がして処理します。
        return await Task.Run(() =>
        {
            // 非同期のキャンセル要求をチェック
            cancellationToken.ThrowIfCancellationRequested();

            // TdmsWrapper の生成とデータ読み出し
            // ※ TdmsWrapper が IDisposable なので、読み込み完了後に確実に解放します。
            using var wrapper = new TdmsWrapper(key.FilePath);
            wrapper.OpenFile();
            var ch = wrapper.GetChannelByName(key.GroupName, key.ChannelName);

            // ネイティブ DLL (nilibdcc.dll) を介して、
            // IMemoryOwner に裏打ちされた型安全な構造体をファクトリします。
            return wrapper.ReadChannel(ch);

        }, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<double> ReadChannelDataStreamAsync(
        string filePath,
        string group,
        string channel,
        nuint chunkSize = 10000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 処理をスレッドプールに逃がす
        await Task.Yield();

        using var file = new TdmsWrapper(filePath);
        file.OpenFile();
        var chHandle = file.GetChannelByName(group, channel);
        if (chHandle.IsInvalid) { yield break; }

        ulong total = file.GetNumDataValues(chHandle);
        double[] buffer = System.Buffers.ArrayPool<double>.Shared.Rent((int)chunkSize);
        try
        {
            for (nuint offset = 0; offset < total; offset += chunkSize)
            {
                // キャンセル要求の確認
                ct.ThrowIfCancellationRequested();

                ulong remaining = total - offset;
                nuint count = (nuint)Math.Min(chunkSize, remaining);
                ReadToBuffer(file, chHandle, offset, count, buffer);
                for (int i = 0; i < (int)count; i++)
                {
                    yield return buffer[i];
                }
            }
        }
        finally
        {
            System.Buffers.ArrayPool<double>.Shared.Return(buffer);
        }
    }
    private static void ReadToBuffer(TdmsWrapper file, ChannelHandle channel, nuint offset, nuint count, double[] destination)
    {
        using var dataOwner = file.GetDataValues(channel, offset, count);
        dataOwner.Memory.Span.CopyTo(destination);

#if false
        using TdmsData tdmsData = file.ReadChannel(channel);
        // 2. パターンマッチングで、ゼロアロケーションの Span<T> を安全に開示
        switch (tdmsData)
        {
            case TdmsData.Double(var owner, var length):
                ReadOnlySpan<double> doubleSpan = owner.Memory.Span[..length];
                doubleSpan.CopyTo(destination);
                // WPFのチャートや、統計計算ロジックにSpanのまま渡す（超高速）
                //ProcessSignal(doubleSpan);
                break;

            case TdmsData.Timestamp(var owner, var length):
                ReadOnlySpan<DateTime> timeSpan = owner.Memory.Span[..length];
                //ProcessTimeline(timeSpan);
                break;

            case TdmsData.String(var values):
                // 文字列配列の処理
                //ProcessLabels(values);
                break;

            case TdmsData.Empty:
                // データが0件の場合
                break;
        }
#endif
    }
}
