// ────────────────────────────────
//
// ────────────────────────────────

using System.Windows;
using ScottPlot;
using WpfUI.Core.Abstracts;
using WpfUI.Core.Base;

namespace WpfUI.Features.Waveform;

public partial class WaveformView : ViewBase<WaveformViewModel>
{
    public WaveformView() : base()
    {
        InitializeComponent();
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is WaveformViewModel vm)
        {
            vm.PlotRequested = async (info, axis, stream) =>
                await AddChannelToPlotAsync(info, axis, stream);
        }
    }
    protected override void OnViewModelAttached(WaveformViewModel? viewModel)
    {
        if (viewModel is null) return;
        SetupPlotLayout();
    }
    private void SetupPlotLayout()
    {
        // グラスフィルデザインに合わせたチャートの外観設定
        var plot = MainPlot.Plot;
        plot.FigureBackground.Color = Colors.Transparent; // 背景透過
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(20, 255, 255, 255);
        plot.DataBackground.Color = ScottPlot.Color.FromColor(drawingColor);

        // グリッドと軸のネオンカラー設定
        plot.Axes.Color(Colors.Gray);
        plot.Grid.MajorLineColor = Colors.Gray.WithAlpha(0.2);

        // 凡例をモダンな位置に配置
        plot.ShowLegend(Alignment.UpperRight);
        plot.Legend.BackgroundColor = Colors.Black.WithAlpha(0.5);
        plot.Legend.FontColor = Colors.White;

        MainPlot.Refresh();
    }
    public async Task AddChannelToPlotAsync(TdmsChannelInfo info, AxisType axisType, IAsyncEnumerable<double> dataStream)
    {
        var plot = MainPlot.Plot;

        // DataLogger (ScottPlot 5 で動的更新に最も最適化されたプロット型)
        var logger = plot.Add.DataLogger();
        logger.LegendText = info.ChannelName;

        // 軸のアサイン
        ConfigureAxis(logger, axisType, info.ChannelName);

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
        plot.Axes.AutoScale();
        MainPlot.Refresh();
    }
    private void ConfigureAxis(ScottPlot.Plottables.DataLogger logger, AxisType axisType, string label)
    {
        var plot = MainPlot.Plot;
        switch (axisType)
        {
            case AxisType.LeftY:
                logger.Axes.YAxis = plot.Axes.Left;
                plot.Axes.Left.Label.Text = label;
                plot.Axes.Left.Label.ForeColor = ScottPlot.Colors.Cyan;
                break;
            case AxisType.RightY:
                // 右軸を有効化
                plot.Axes.Right.IsVisible = true;
                logger.Axes.YAxis = plot.Axes.Right;
                plot.Axes.Right.Label.Text = label;
                plot.Axes.Right.Label.ForeColor = ScottPlot.Colors.Magenta;
                break;
            case AxisType.X:
                // 特殊ケース：X軸基準データの入れ替え
                break;
        }
    }
}
