// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Windows;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Documents.Charts;

public partial class SkeletonDocChartView : ViewBase<SkeletonDocChartViewModel>
{
    private IDisposable? _chartSubscription;

    public SkeletonDocChartView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(SkeletonDocChartViewModel? viewModel)
    {
        if (viewModel is null) return;
        _chartSubscription?.Dispose();

        // ViewModel側のデータ更新をフックして、ScottPlot5 の公式推奨描画パターンを安全に実行
        _chartSubscription = viewModel.DataUpdatedStream
            .ObserveOnCurrentDispatcher()
            .Subscribe(dataPoints =>
            {
                if (dataPoints == null || dataPoints.Xs.Length == 0) return;

                // 1. 推奨のクリーンなインスタンス生成
                ScottPlot.Plot plot = new();

                // 2. 正確な ScottPlot 5 描画 API のコール
                var scatter = plot.Add.Scatter(dataPoints.Xs, dataPoints.Ys);
                scatter.MarkerStyle.Size = 4;

                // 3. 厳格に定義された Axes / Interaction プロパティの設定
                plot.Axes.Color(ScottPlot.Color.FromHex("#94A3B8"));
                //plot.Grid.MajorGridColor = ScottPlot.Color.FromHex("#1E293B");

                // ダークテーマ用にキャンバスを調整
                plot.FigureBackground.Color = ScottPlot.Color.FromHex("#0F111A");
                plot.DataBackground.Color = ScottPlot.Color.FromHex("#11131E");

                // 4. コントロールへアサインし再描画
                //MainPlot.Plot = plot;
                MainPlot.Refresh();
            });
    }
}
