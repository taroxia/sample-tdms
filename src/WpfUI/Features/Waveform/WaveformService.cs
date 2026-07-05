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
using WpfUI.Core.Domain.Types;
using WpfUI.Infrastructure.Persistence.Tdms;

namespace WpfUI.Features.Waveform;

// 軸の配置定義
public enum AxisPosition { X, Left, Right }

// どのチャンネルがどの位置にアサインされているかのトポロジー情報
public record PlotAssignment(string LayerId, TdmsChannelMetadata Channel, AxisPosition Position, string AxisId)
{
    public ScottPlot.CoordinateRange YRange { get; set; } = ScottPlot.CoordinateRange.NotSet;
}

// 1つの段（プロットレイヤー）を定義するモデル
public class PlotLayerModel
{
    public string LayerId { get; } = Guid.NewGuid().ToString("N");  // fromat;Number.
    public ObservableCollection<PlotAssignment> Assignments { get; } = new();
    public ScottPlot.AxisLimits CurrentLimits { get; set; } = ScottPlot.AxisLimits.NoLimits;
}

public sealed partial class WaveformService : BaseService
{
    private readonly object _lock = new();
    private readonly ITdmsService _tdmsService;

    // --- トポロジー（レイアウト構造）の永続管理 (Singleton) ---
    public ObservableCollection<PlotLayerModel> PlotLayers { get; } = new();

    // X軸の共有同期用
    public ReactiveProperty<ScottPlot.AxisLimits> SharedAxisLimits { get; } = new(new ScottPlot.AxisLimits(double.NaN, double.NaN, double.NaN, double.NaN));  // Left, Right, Buttom, Top.
    public ReactiveProperty<ScottPlot.CoordinateRange> SharedXLimits { get; } = new(new ScottPlot.CoordinateRange(double.NaN, double.NaN));  // Left, Right.

    public WaveformService(ITdmsService tdms)
    {
        _tdmsService = tdms;

        InitializePipeline();
        InitializeExplorerPipeline();
        InitializeGraphPipeline();

        // 初期状態として最初の1段を常設
        PlotLayers.Add(new PlotLayerModel());
    }
    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializePipeline()
    {
        SharedXLimits.AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
        SharedAxisLimits.Dispose();
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
}
