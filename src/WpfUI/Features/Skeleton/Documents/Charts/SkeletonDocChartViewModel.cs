// ────────────────────────────────
//
// ────────────────────────────────

using System;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Documents.Charts;

// 描画データをカプセル化する構造体
public record struct ChartDataPoints(double[] Xs, double[] Ys);

public sealed class SkeletonDocChartViewModel : FeatureViewModelBase
{
    private readonly Subject<ChartDataPoints> _dataUpdatedStream = new();
    public Observable<ChartDataPoints> DataUpdatedStream => _dataUpdatedStream;

    public BindableReactiveProperty<string> ChartTitle { get; }

    public SkeletonDocChartViewModel(SkeletonService service)
    {
        ChartTitle = new BindableReactiveProperty<string>("Signal Stream: Idle").AddTo(ref _disposables);
    }

    public void LoadNodeStream(string nodeName)
    {
        ChartTitle.Value = $"Signal Stream: {nodeName}";

        // 商用シミュレーションデータの生成 (ScottPlot 5 Scatter用)
        const int pointCount = 200;
        double[] xs = new double[pointCount];
        double[] ys = new double[pointCount];
        Random rand = new();

        for (int i = 0; i < pointCount; i++)
        {
            xs[i] = i;
            ys[i] = Math.Sin(i * 0.1) + (rand.NextDouble() - 0.5) * 0.2;
        }

        // 描画ストリームへプッシュ
        _dataUpdatedStream.OnNext(new ChartDataPoints(xs, ys));
    }

    protected override void OnDisposed()
    {
        ChartTitle.Dispose();
        _dataUpdatedStream.Dispose();
    }
}
