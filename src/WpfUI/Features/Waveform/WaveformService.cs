// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using R3;

using WpfUI.Core.Abstractions;
using WpfUI.Core.Base;
using WpfUI.Core.Collections;
using WpfUI.Core.Dmain.Models;
using WpfUI.Infrastructure.Persistence.Tdms;

namespace WpfUI.Features.Waveform;

// 軸の配置定義
public enum AxisPosition { X, Left, Right }

// どのチャンネルがどの位置にアサインされているかのトポロジー情報
public record PlotAssignment(TdmsChannelMetadata Channel, AxisPosition Position, string AxisId);

// 1つの段（プロットレイヤー）を定義するモデル
public class PlotLayerModel
{
    public string LayerId { get; } = Guid.NewGuid().ToString("N");
    public ObservableCollection<PlotAssignment> Assignments { get; } = new();
}

public sealed partial class WaveformService : BaseService
{
    private readonly object _lock = new();
    private readonly ITdmsService _tdmsService;

    // --- トポロジー（レイアウト構造）の永続管理 (Singleton) ---
    public ObservableCollection<PlotLayerModel> PlotLayers { get; } = new();

    // X軸の共有同期用
    public ReactiveProperty<ScottPlot.AxisLimits> SharedAxisLimits { get; } = new(new ScottPlot.AxisLimits(0, 10, 0, 10));

    public WaveformService(ITdmsService tdms)
    {
        _tdmsService = tdms;

        InitializeExplorerPipeline();
        InitializeGraphPipeline();

        // 初期状態として最初の1段を常設
        PlotLayers.Add(new PlotLayerModel());
    }

    // 新しい段を追加
    public void AddNewLayer()
    {
        PlotLayers.Add(new PlotLayerModel());
    }

    // 特定の段を削除（最低1つは残す）
    public void RemoveLayer(PlotLayerModel layer)
    {
        if (PlotLayers.Count > 1)
        {
            PlotLayers.Remove(layer);
        }
        else
        {
            layer.Assignments.Clear();
        }
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

    public override void Dispose()
    {
        SharedAxisLimits.Dispose();
        base.Dispose();
    }
}
