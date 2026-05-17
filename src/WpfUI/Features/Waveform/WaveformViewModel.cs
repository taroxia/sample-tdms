// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using R3;
using R3.Collections;
using ScottPlot;
using ScottPlot.WPF;
using WpfUI.Core.Abstracts;
using WpfUI.Core.Base;

namespace WpfUI.Features.Waveform;

public sealed class WaveformViewModel : ViewModelBase
{
    private readonly WaveformStateService _service;
    private readonly ITdmsService _tdmsService;
    public ReactiveCommand<DragEventArgs> DropToChartCommand { get; } = new();

    public Func<TdmsChannelInfo, AxisType, IAsyncEnumerable<double>, Task>? PlotRequested { get; set; }

    public ReactiveProperty<AxisLimits> CurrentAxisLimits { get; }
    public ReactiveCommand<AxisLimits> AxisChangedCommand { get; } = new();
    public List<ScottPlot.Plottables.SignalXY> ActiveSignals { get; } = [];

    public BindableReactiveProperty<double[]?> Xs { get; }

    public ReadOnlyObservableCollection<double[]> LeftYs { get; }

    public Observable<Unit> PlotRefreshRequested => _plotRefreshTrigger;
    private readonly Subject<Unit> _plotRefreshTrigger = new();

    public WaveformViewModel(WaveformStateService service, ITdmsService tdmsService)
    {
        _tdmsService = tdmsService;
        _service = service;

        CurrentAxisLimits = service.CurrentAxisLimits;

        // Xs.
        Xs = new BindableReactiveProperty<double[]?>(service.Xs.Value).AddTo(_disposables);
        service.Xs.Skip(1).Subscribe(v => Xs.Value = v).AddTo(_disposables);
        Xs.Skip(1).Subscribe(v => service.Xs.Value = v).AddTo(_disposables);

        LeftYs = new ReadOnlyObservableCollection<double[]>(service.LeftYs);



        DropToChartCommand.SubscribeAwait(async (e, ct) => await OnDropToChartAsync(e, ct))
            .AddTo(_disposables);

        // from View.
        AxisChangedCommand.Subscribe(limits => CurrentAxisLimits.Value = limits)
            .AddTo(_disposables);

        Xs.Subscribe(item =>
        {
            RebuildSignals(item, LeftYs.ToList()!);
            _plotRefreshTrigger.OnNext(Unit.Default);
        })
        .AddTo(_disposables);

        service.OnLeftYsChanged
        .Where(e => e.Action == NotifyCollectionChangedAction.Add) // 追加時のみ
        .Subscribe(e =>
        {
            RebuildSignals(Xs.Value!, e.NewItems);
            _plotRefreshTrigger.OnNext(Unit.Default);
        })
        .AddTo(_disposables);

    }


    private async Task OnDropToChartAsync(DragEventArgs e, CancellationToken ct)
    {
        if (e.Data.GetData(typeof(List<TdmsChannelInfo>)) is not List<TdmsChannelInfo> channels) return;

        var axis = DetermineAxis(
            e.GetPosition((IInputElement)e.Source),
            ((FrameworkElement)e.Source).ActualWidth,
            ((FrameworkElement)e.Source).ActualHeight);


        //double[] dataX = { 1, 2, 3, 4, 5 };
        //double[] dataY = { 1, 4, 9, 16, 25 };
        //if (axis == AxisType.X)
        //{
        //    Xs.Value = dataX;
        //}
        //else
        //{
        //    _service.AppendLeftYs(dataY);
        //}

        //return;
        foreach (var ch in channels)
        {
            // Serviceからストリームを取得
            var stream = _tdmsService.ReadChannelDataStreamAsync(ch.FilePath, ch.GroupName, ch.ChannelName);

            // Viewに描画を依頼
            if (PlotRequested != null)
            {
                await PlotRequested.Invoke(ch, axis, stream);
            }
        }
    }

    public static List<double[]> ConvertToDoubleArrayList(IList sourceList)
    {
        ArgumentNullException.ThrowIfNull(sourceList);
        var result = new List<double[]>();
        lock (sourceList.SyncRoot) // IList.SyncRoot: .NET標準
        {
            foreach (object item in sourceList)
            {
                if (item is double[] doubleArray)
                {
                    result.Add((double[])doubleArray.Clone());
                }
                else if (item is IEnumerable enumerable)
                {
                    try
                    {
                        double[] converted = enumerable.Cast<double>().ToArray();
                        result.Add(converted);
                    }
                    catch (InvalidCastException)
                    {
                        continue;
                    }
                }
            }
        }
        return result;
    }


    private void RebuildSignals(double[] xsValue, IList? newItems)
    {
        if (newItems is null) { return; }

        var ys = ConvertToDoubleArrayList(newItems!);
        RebuildSignals(xsValue, ys!);
    }

    private void RebuildSignals(double[]? xs, List<double[]?>? ys)
    {
        ActiveSignals.Clear();
        if (xs is null || ys is null || ys.Count == 0 || xs.Length == 0)
            return;

        // Y軸マルチデータ対応。データの整合性チェックを行いつつSignalXYを生成
        foreach (var yData in ys)
        {
            if (yData is null || yData.Length != xs.Length) continue;

            // ScottPlot 5でのSignalXY生成（メモリ効率が最も高い描画方式）
            var dataSource = new ScottPlot.DataSources.SignalXYSourceDoubleArray(xs, yData);
            var signal = new ScottPlot.Plottables.SignalXY(dataSource);

            // 商用向けモダンデザインに合わせた線の太さ調整など
            signal.LineWidth = 2;

            ActiveSignals.Add(signal);
        }
    }


    private async Task LoadAndPlotAsync(TdmsChannelInfo info, AxisType axis)
    {
        // チャンクごとに読み込み、ScottPlotの DataLogger 等に流し込む
        // DataLoggerは ScottPlot 5 でリアルタイム更新に最適なプロットタイプです
        var logger = new List<double>();

        await foreach (var value in _tdmsService.ReadChannelDataStreamAsync(info.FilePath, info.GroupName, info.ChannelName))
        {
            logger.Add(value);

            // 1万点ごとに再描画要求（パフォーマンス最適化）
            if (logger.Count % 10000 == 0)
            {
                // Viewへの通知イベントなどを発火
            }
        }
    }
    public async Task HandleDropAsync(DragEventArgs e, WpfPlot plotControl)
    {
        var data = e.Data.GetData(typeof(List<TdmsChannelInfo>)) as List<TdmsChannelInfo>;
        if (data is null) return;

        // ドロップ座標から軸を判定
        var position = e.GetPosition(plotControl);
        var axisType = DetermineAxis(position, plotControl.ActualWidth, plotControl.ActualHeight);

        foreach (var info in data)
        {
            // ScottPlot 5 の DataLogger (動的更新に強い) を生成
            var logger = plotControl.Plot.Add.DataLogger();

            // 軸設定
            switch (axisType)
            {
                case AxisType.RightY:
                    logger.Axes.YAxis = plotControl.Plot.Axes.Right;
                    plotControl.Plot.Axes.Right.Label.Text = info.ChannelName;
                    break;
                case AxisType.LeftY:
                    logger.Axes.YAxis = plotControl.Plot.Axes.Left;
                    plotControl.Plot.Axes.Left.Label.Text = info.ChannelName;
                    break;
                    // X軸へのドロップは、既存プロットのXデータ差し替え等の特殊処理
            }

            // 非同期ストリーミング読み込みと描画更新
            await foreach (var value in _tdmsService.ReadChannelDataStreamAsync(info.FilePath, info.GroupName, info.ChannelName))
            {
                logger.Add(value);

                // チャンク単位での描画リフレッシュ（高効率）
                //if (logger.Data.Count % 5000 == 0)
                //if (logger.Data. .GetAllPoints().Count % 5000 == 0)
                //{
                //    plotControl.Refresh();
                //    await Task.Yield(); // UIスレッドの占有を防止
                //}
            }
            plotControl.Refresh();
        }
    }


    private AxisType DetermineAxis(Point p, double w, double h)
    {
        if (p.Y > h - 40) return AxisType.X;
        if (p.X < 60) return AxisType.LeftY;
        if (p.X > w - 60) return AxisType.RightY;
        return AxisType.LeftY; // Default
    }
}

public enum AxisType { X, LeftY, RightY }
