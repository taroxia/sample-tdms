// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WpfUI.Core.Abstractions;
using WpfUI.Core.Base;
using WpfUI.Core.Collections;
using WpfUI.Core.Domain.Types;
using WpfUI.Infrastructure.Persistence.Tdms;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Features.Waveform;

public sealed partial class WaveformService
{
    // ----------------------------------------------------------------
    // Properties / Commands
    // ----------------------------------------------------------------

    private readonly LruCache<TdmsCacheKey, TdmsData> _cache = new(capacity: 20);

    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializeGraphPipeline()
    { }

    // ----------------------------------------------------------------
    // Public Logic Methods
    // ----------------------------------------------------------------

    public async ValueTask<CacheLease<TdmsData>> GetChannelDataAsync(TdmsChannelMetadata channel, CancellationToken ct = default)
    {
        return await _cache.GetOrAddAsync(channel.CacheKey, async key =>
        {
            return await _tdmsService.ReadChannelDataStreamAsync(key, ct);
        }).ConfigureAwait(false);
    }
    public void ClearCache()
    {
        _cache.Clear();
    }

    // TdmsDataからScottPlot5用のdouble配列へ安全に高速変換するヘルパー（IMemoryOwnerのライフサイクルに準拠）
    public double[] ConvertToDoubleArray(TdmsData data)
    {
        return data switch
        {
            TdmsData.Double d => CopyDoubleArray(d.Owner.Memory.Span, d.Length),
            TdmsData.Float f => ToDoubleArray<float>(f.Owner.Memory.Span, f.Length),
            TdmsData.Int32 i => ToDoubleArray<int>(i.Owner.Memory.Span, i.Length),
            TdmsData.Int16 s => ToDoubleArray<short>(s.Owner.Memory.Span, s.Length),
            TdmsData.UInt8 b => ToDoubleArray<byte>(b.Owner.Memory.Span, b.Length),
            TdmsData.Timestamp t => ToDoubleArray(t.Owner.Memory.Span, t.Length),
            _ => Array.Empty<double>()
        };
    }

    // ----------------------------------------------------------------
    // Private Helper Methods
    // ----------------------------------------------------------------

    private static double[] CopyDoubleArray(ReadOnlySpan<double> span, int length)
    {
        double[] result = new double[length];
        span[..length].CopyTo(result);
        return result;
    }
    private static double[] ToDoubleArray<T>(ReadOnlySpan<T> span, int length) where T : struct, INumber<T>
    {
        double[] result = new double[length];
        ReadOnlySpan<T> sliced = span[..length];
        for (int i = 0; i < sliced.Length; i++)
        {
            result[i] = double.CreateChecked(sliced[i]);
        }
        return result;
    }
    private static double[] ToDoubleArray(ReadOnlySpan<DateTime> span, int length)
    {
        double[] result = new double[length];
        ReadOnlySpan<DateTime> sliced = span[..length];

        for (int i = 0; i < sliced.Length; i++)
        {
            // ScottPlot 5 でも日時は ToOADate (Numeric 値) としてプロット内部で処理されます
            result[i] = sliced[i].ToOADate();
        }
        return result;
    }
}
