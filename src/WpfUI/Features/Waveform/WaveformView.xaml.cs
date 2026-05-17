// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using R3;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using WpfUI.Core.Abstracts;
using WpfUI.Core.Base;

namespace WpfUI.Features.Waveform;

public partial class WaveformView : ViewBase<WaveformViewModel>
{
    private readonly CompositeDisposable _disposables = new();
    private WaveformViewModel? _viewModel;

    public WaveformView() : base()
    {
        InitializeComponent();

        DataContextChanged += WaveformView_DataContextChanged;
        Unloaded += (s, e) => _disposables.Clear();
    }
    protected override void OnViewModelAttached(WaveformViewModel? vm)
    {
        if (DataContext is not WaveformViewModel viewModel) return;
        if (vm is null) return;

        _viewModel = vm;

        //MainPlot.Reset(vm.SharedPlot);

        /*
        Observable.CombineLatest(vm.Xs, vm.Ys, (x, y) => (x, y))
            .Subscribe(this, static (data, state) =>
            {
                //var targetView = state;
                //targetView.MainPlot?.Plot.Clear();

                //var (x, y) = data;
                //if (x is not null && y is not null)
                //{
                //}
                //else
                //{
                //    targetView.MainPlot.Plot.Axes.SetLimits(0, 100, -10, 10);
                //}
                //if (_isInitialLoad)
                //{
                //    if (vm.CurrentLimits.Value != AxisLimits.NoLimits)
                //    {
                //        targetView.MainPlot.Plot.Axes.SetLimits(vm.CurrentLimits.Value);

                //        var signal = targetView.MainPlot.Plot.Add.SignalXY(x, y);
                //        signal.Color = Color.FromHex("#00A2E8");
                //        signal.LineWidth = 1.5f;

                //        // ViewModel に保存されていた前回の表示範囲を復元
                //        var (min, max) = viewModel.XLimits.Value;
                //        targetView.MainPlot.Plot.Axes.SetLimitsX(min, max);

                //    }
                //    else
                //    {
                //        targetView.MainPlot.Plot.Axes.Autoscale(); // 過去データがなければ全体表示
                //    }
                //    _isInitialLoad = false;
                //}
                //targetView.MainPlot.Refresh();

            })
            .AddTo(_disposables);
        */


        MainPlot.Plot.RenderManager.AxisLimitsChanged += (_, _) =>
        {
            // 現在の表示範囲をViewModelにバックアップ（画面が切り替わってもこれで安心）
            vm.AxisChangedCommand.Execute(MainPlot.Plot.Axes.GetLimits());
        };

        //Observable.FromEvent<AxisLimits>(
        //        h => MainPlot.Plot.RenderManager.AxisLimitsChanged += h,
        //        h => MainPlot.Plot.RenderManager.AxisLimitsChanged -= h)
        //    .ThrottleFirst(TimeSpan.FromMilliseconds(100)) // 負荷軽減のための間引き
        //    .Subscribe(this, static (args, state) =>
        //    {
        //    })
        //    .AddTo(_disposables);
    }
    private void WaveformView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not WaveformViewModel vm) { return; }

        _viewModel = vm;
        //MainPlot.Reset(vm.SharedPlot);
        vm.PlotRefreshRequested
            .Subscribe(_ => OnPlotRefresh(vm))
            .AddTo(_disposables);

        if (!vm.CurrentAxisLimits.Value.Equals(AxisLimits.NoLimits))
        {
            MainPlot.Plot.Axes.SetLimits(vm.CurrentAxisLimits.Value);
        }

        OnPlotRefresh(vm);
        //MainPlot.Refresh();
    }
    public void OnPlotRefresh(WaveformViewModel vm)
    {
        MainPlot.Plot.Clear();

        foreach (var signal in vm.ActiveSignals)
        {
            MainPlot.Plot.Add.Plottable(signal);
        }

        if (vm.CurrentAxisLimits.Value.Equals(AxisLimits.NoLimits))
        {
            MainPlot.Plot.Axes.AutoScale();
        }
        else
        {
            MainPlot.Plot.Axes.SetLimits(vm.CurrentAxisLimits.Value);
        }
        MainPlot.Refresh();
    }


    public void OnPlotRendered(object sender, EventArgs e)
    {
    }




    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        //if (DataContext is WaveformViewModel vm)
        //{
        //    vm.PlotRequested = async (info, axis, stream) =>
        //        await AddChannelToPlotAsync(info, axis, stream);
        //}
    }

    /*
    private void UpdatePlot(WaveformViewModel vm)
    {
        // 描画クリア
        MainPlot.Plot.Clear();

        var xs = vm.Xs.Value;
        var ysList = vm.Ys.Value;

        if (xs == null || ysList == null || ysList?.Count == 0)
        {
            MainPlot.Refresh();
            return;
        }

        // 複数ラインのプロット
        foreach (var ys in ysList)
        {
            if (ys == null || xs.Length != ys.Length) continue;

            // ScottPlot 5 の SignalXY は大容量データの描画に最適化されています
            var sig = MainPlot.Plot.Add.SignalXY(xs, ys);
            sig.MarkerSize = 0; // 高速化のためマーカーは非表示
            sig.LineWidth = 1.5f;
        }

        if (vm.CurrentAxisLimits.Value.Equals(AxisLimits.NoLimits))
        {
            MainPlot.Plot.Axes.AutoScale();
            //MainPlot.Plot.Axes.SetLimits(vm.CurrentAxisLimits.Value);
        }
        MainPlot.Refresh();
    }
    */




    //protected override void OnViewModelAttached(WaveformViewModel? viewModel)
    //{
    //    if (viewModel is null) return;
    //    SetupPlotLayout();
    //}
    private void SetupPlotLayout()
    {
        // グラスフィルデザインに合わせたチャートの外観設定
        var plot = MainPlot.Plot;
        MainPlot.Plot.FigureBackground.Color = Colors.Transparent; // 背景透過
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(20, 255, 255, 255);
        MainPlot.Plot.DataBackground.Color = ScottPlot.Color.FromColor(drawingColor);

        // グリッドと軸のネオンカラー設定
        MainPlot.Plot.Axes.Color(Colors.Gray);
        MainPlot.Plot.Grid.MajorLineColor = Colors.Gray.WithAlpha(0.2);

        // 凡例をモダンな位置に配置
        MainPlot.Plot.ShowLegend(Alignment.UpperRight);
        MainPlot.Plot.Legend.BackgroundColor = Colors.Black.WithAlpha(0.5);
        MainPlot.Plot.Legend.FontColor = Colors.White;

        MainPlot.Refresh();
    }
    public async Task AddChannelToPlotAsync(TdmsChannelInfo info, AxisType axisType, IAsyncEnumerable<double> dataStream)
    {
        double[] dataX = { 1, 2, 3, 4, 5 };
        double[] dataY = { 1, 4, 9, 16, 25 };
        double[] data = axisType == AxisType.X ? dataX : dataY;

        //var signal = MainPlot.Plot.GetPlottables<SignalXY>() ?? MainPlot.Plot.Add.SignalXY(null, null);
        //signal.Axes.XAxis = axisType == AxisType.X ? MainPlot.Plot.Axes.Bottom : MainPlot.Plot.Axes.Left;


        //newPlot.Plot.Add.Scatter(dataX, dataY);

        //signal.Title("TDMS High-Speed Analysis");
        MainPlot.Plot.Axes.AutoScale();
        MainPlot.Refresh();
    }

    /*
    public async Task AddChannelToPlotAsync(TdmsChannelInfo info, AxisType axisType, IAsyncEnumerable<double> dataStream)
    {
        var plot = MainPlot.Plot;

        // DataLogger (ScottPlot 5 で動的更新に最も最適化されたプロット型)
        var logger = MainPlot.Plot.Add.DataLogger();
        logger.LegendText = info.ChannelName;

        double[] result = await dataStream.ToObservable().ToArrayAsync();
        var signal = MainPlot.Plot.Add.Signal(result);

        // 軸のアサイン
        ConfigureAxis(signal, axisType, info.ChannelName);

        // ストリーミング描画
        int count = 0;
        await foreach (var value in dataStream)
        {
            logger.Add(count++, value);

            // 5000点ごとにリフレッシュすることで描画負荷を軽減しつつリアルタイム性を確保
            if (count % 5000 == 0)
            {
                MainPlot.Refresh();
                await Task.Yield(); // UIの入力を妨げない
            }
        }

        // 最後に最適化して全表示
        MainPlot.Plot.Axes.AutoScale();
        MainPlot.Refresh();
    }
    */
    private void ConfigureAxis(Signal signal, AxisType axisType, string label)
    {
        var plot = MainPlot.Plot;
        switch (axisType)
        {
            case AxisType.LeftY:
            {
                var axis = MainPlot.Plot.Axes.Left;
                axis.Label.Text = label;
                axis.Label.ForeColor = ScottPlot.Colors.Cyan;
                signal.Axes.YAxis = axis;
            }
            break;
            case AxisType.RightY:
            {
                var axis = MainPlot.Plot.Axes.Right;
                axis.IsVisible = true;
                axis.Label.Text = label;
                axis.Label.ForeColor = ScottPlot.Colors.Magenta;
                signal.Axes.YAxis = axis;
            }
            break;
            case AxisType.X:
                signal.Axes.XAxis = MainPlot.Plot.Axes.Bottom;
                // 特殊ケース：X軸基準データの入れ替え
                break;
        }
    }
    /*
    private void ConfigureAxis(ScottPlot.Plottables.DataLogger logger, AxisType axisType, string label)
    {
        var plot = MainPlot.Plot;
        switch (axisType)
        {
            case AxisType.LeftY:
                logger.Axes.YAxis = MainPlot.Plot.Axes.Left;
                MainPlot.Plot.Axes.Left.Label.Text = label;
                MainPlot.Plot.Axes.Left.Label.ForeColor = ScottPlot.Colors.Cyan;
                break;
            case AxisType.RightY:
                // 右軸を有効化
                MainPlot.Plot.Axes.Right.IsVisible = true;
                logger.Axes.YAxis = MainPlot.Plot.Axes.Right;
                MainPlot.Plot.Axes.Right.Label.Text = label;
                MainPlot.Plot.Axes.Right.Label.ForeColor = ScottPlot.Colors.Magenta;
                break;
            case AxisType.X:
                // 特殊ケース：X軸基準データの入れ替え
                break;
        }
    }
    */
}
