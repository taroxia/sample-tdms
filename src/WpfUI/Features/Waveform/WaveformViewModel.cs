// ────────────────────────────────
//
// ────────────────────────────────

using System.IO;
using System.Windows;
using OpenTK.Audio.OpenAL;
using R3;
using ScottPlot.WPF;
using WpfUI.Core.Abstracts;
using WpfUI.Core.Base;

namespace WpfUI.Features.Waveform;

public sealed class WaveformViewModel : ViewModelBase
{
    private readonly ITdmsService _tdmsService;
    public ReactiveCommand<DragEventArgs> DropToChartCommand { get; } = new();

    public Func<TdmsChannelInfo, AxisType, IAsyncEnumerable<double>, Task>? PlotRequested { get; set; }


    // ScottPlotの参照はView側で操作するため、Action経由でブリッジするか
    // ここではロジックのコアを示します
    public WaveformViewModel(ITdmsService tdmsService)
    {
        _tdmsService = tdmsService;

        DropToChartCommand.SubscribeAwait(async (e, ct) => await OnDropToChartAsync(e, ct));
    }

    private async Task OnDropToChartAsync(DragEventArgs e, CancellationToken ct)
    {
        if (e.Data.GetData(typeof(List<TdmsChannelInfo>)) is not List<TdmsChannelInfo> channels) return;

        var axis = DetermineAxis(
            e.GetPosition((IInputElement)e.Source),
            ((FrameworkElement)e.Source).ActualWidth,
            ((FrameworkElement)e.Source).ActualHeight);

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
