// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using R3;

using WpfUI.Core.Base;
using WpfUI.Core.Collections;
using WpfUI.Core.Domain.Types;
using WpfUI.Infrastructure.Persistence.Tdms;

namespace WpfUI.Features.Waveform;

public sealed class WaveformViewModel : FeatureViewModelBase
{
    private readonly WaveformService _service;

    // ----------------------------------------------------------------
    // Properties / Commands
    // ----------------------------------------------------------------

    public ObservableCollection<PlotLayerModel> PlotLayers => _service.PlotLayers;
    public ReactiveProperty<ScottPlot.AxisLimits> SharedAxisLimits => _service.SharedAxisLimits;
    public ReactiveProperty<ScottPlot.CoordinateRange> SharedXLimits => _service.SharedXLimits;

    public ReactiveCommand AddLayerCommand { get; private set; } = new();
    public ReactiveCommand<PlotLayerModel> RemoveLayerCommand { get; private set; } = new();
    public ReactiveCommand<PlotAssignment> RemoveAssignmentCommand { get; private set; }

    // グラフ再描画をViewへ通知するためのイベントストリーム
    private readonly Subject<Unit> _requestRender = new();
    public Observable<Unit> RequestRender => _requestRender;

    // ----------------------------------------------------------------
    // Constructor
    // ----------------------------------------------------------------

    public WaveformViewModel(WaveformService service)
    {
        _service = service;

        InitializePipeline();
    }

    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializePipeline()
    {
        AddLayerCommand
            .Subscribe(_ =>
            {
                _service.AddNewLayer();
                _requestRender.OnNext(Unit.Default);
            }).AddTo(ref _disposables);

        RemoveLayerCommand
            .Subscribe(layer =>
            {
                _service.RemoveLayer(layer);
                _requestRender.OnNext(Unit.Default);
            }).AddTo(ref _disposables);

        RemoveAssignmentCommand = new ReactiveCommand<PlotAssignment>();

        RemoveAssignmentCommand
            .Subscribe(assignment =>
            {
                if (assignment == null) return;

                if (assignment.Position == AxisPosition.X)
                {
                    foreach (var targetLayer in PlotLayers)
                    {
                        targetLayer.Assignments.Remove(assignment);
                    }
                }
                else
                {
                    // assignment 内に LayerId があるため、O(1) または高速な LINQ でレイヤーを特定可能
                    var targetLayer = PlotLayers.FirstOrDefault(l => l.LayerId == assignment.LayerId);
                    if (targetLayer == null) return;

                    targetLayer.Assignments.Remove(assignment);
                }

                //RequestPlotUpdate();
                _requestRender.OnNext(Unit.Default);
            });
    }

    // ----------------------------------------------------------------
    // Public Logic Methods
    // ----------------------------------------------------------------

    protected override void OnDisposed()
    {
        _requestRender.OnCompleted();
    }

    // ViewのDrop処理から呼び出されるコアロジック
    public async Task HandleChannelDropAsync(PlotLayerModel layer, AxisPosition position)
    {
        var currentDict = _service.SelectedExpChannelsMetadata.CurrentValue;
        if (currentDict == null || currentDict.Count == 0) return;

        var existingSet = layer.Assignments
            .Select(a => (a.Channel, a.Position))
            .ToHashSet();

        var newAssignments = currentDict.Values
            .Where(channel => channel != null && !existingSet.Contains((channel, position)))
            .Select(channel => new PlotAssignment(
                LayerId: layer.LayerId,
                Channel: channel,
                Position: position,
                AxisId: Guid.NewGuid().ToString("N")
            // AxisId: $"Axis_{position}_{channel.GetHashCode()}" // ScottPlot 5用のユニークなAxisIdの生成例
            ))
            .ToList();

        if (position == AxisPosition.X)
        {
            if (newAssignments.Count != 0)
            {
                foreach (var targetLayer in PlotLayers)
                {
                    var existingX = targetLayer.Assignments.FirstOrDefault(a => a.Position == AxisPosition.X);
                    if (existingX != null) targetLayer.Assignments.Remove(existingX);
                    targetLayer.Assignments.Add(newAssignments[0]);
                }
            }
        }
        else
        {
            foreach (var assignment in newAssignments)
            {
                layer.Assignments.Add(assignment);
            }
        }
#if false
        if (channel == null) return;

        // X軸は1つの段につき1つのみ、Y軸はマルチアサイン可能とする
        string axisId = Guid.NewGuid().ToString("N");

        if (position == AxisPosition.X)
        {
            var existingX = layer.Assignments.FirstOrDefault(a => a.Position == AxisPosition.X);
            if (existingX != null) layer.Assignments.Remove(existingX);
        }

        var assignment = new PlotAssignment(channel, position, axisId);
        layer.Assignments.Add(assignment);
#endif

        // Navigatorの選択状態をクリア
        _service.ClearExpSelection();
        // Viewへ再レンダリングを要請
        _requestRender.OnNext(Unit.Default);
        await Task.CompletedTask;
    }

    // データ読み込みのプロキシ
    public async ValueTask<double[]> LoadChannelDataAsync(TdmsChannelMetadata channel)
    {
        using var rawData = await _service.GetChannelDataAsync(channel);
        return _service.ConvertToDoubleArray(rawData.Value);
    }
}
