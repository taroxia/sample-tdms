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
using WpfUI.Core.Dmain.Models;
using WpfUI.Infrastructure.Persistence.Tdms;

namespace WpfUI.Features.Waveform;

public sealed class WaveformViewModel : ViewModelBase
{
    private readonly WaveformService _service;

    public ObservableCollection<PlotLayerModel> PlotLayers => _service.PlotLayers;
    public ReactiveProperty<ScottPlot.AxisLimits> SharedAxisLimits => _service.SharedAxisLimits;

    public BindableCommand AddLayerCommand { get; }
    public BindableCommand<PlotLayerModel> RemoveLayerCommand { get; }
    public BindableCommand<(PlotLayerModel Layer, PlotAssignment Assignment)> RemoveAssignmentCommand { get; }

    // グラフ再描画をViewへ通知するためのイベントストリーム
    private readonly Subject<Unit> _requestRender = new();
    public Observable<Unit> RequestRender => _requestRender;

    public WaveformViewModel(WaveformService service)
    {
        _service = service;

        AddLayerCommand = new BindableCommand(_ => _service.AddNewLayer()).AddTo(ref _disposables);
        RemoveLayerCommand = new BindableCommand<PlotLayerModel>(layer => _service.RemoveLayer(layer)).AddTo(ref _disposables);

        RemoveAssignmentCommand = new BindableCommand<(PlotLayerModel Layer, PlotAssignment Assignment)>(pair =>
        {
            pair.Layer.Assignments.Remove(pair.Assignment);
            _requestRender.OnNext(Unit.Default);
        }).AddTo(ref _disposables);
    }

    // ViewのDrop処理から呼び出されるコアロジック
    public async Task HandleChannelDropAsync(PlotLayerModel layer, TdmsChannelMetadata channel, AxisPosition position)
    {
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

    public void Dispose()
    {
        _requestRender.OnCompleted();
        _disposables.Dispose();
    }
}

// シンプルなR3ベースのICommand実装
public sealed class BindableCommand : ICommand, IDisposable
{
    private readonly Action<object?> _execute;
    public event EventHandler? CanExecuteChanged;
    public BindableCommand(Action<object?> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void SystemNotify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    public void Execute(object? parameter) => _execute(parameter);
    public void Dispose() { }
}

public sealed class BindableCommand<T> : ICommand, IDisposable
{
    private readonly Action<T> _execute;
    public event EventHandler? CanExecuteChanged;
    public BindableCommand(Action<T> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) { if (parameter is T t) _execute(t); }
    public void Dispose() { }
}
