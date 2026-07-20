// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.ObjectModel;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Documents.Details;

public record MetricRow(string PropertyName, string Value);

public sealed class SkeletonDocDetailViewModel : ViewModelBase
{
    // UniRxのReactiveCollection等の混入を徹底排除し、標準のObservableCollectionで安全に定義
    public ObservableCollection<MetricRow> AnalyticalMetrics { get; }

    public SkeletonDocDetailViewModel(SkeletonService service)
    {
        AnalyticalMetrics = new ObservableCollection<MetricRow>
        {
            new ("Max Peak Amplitude", "1.245 V"),
            new ("Root Mean Square (RMS)", "0.707 V"),
            new ("Signal-to-Noise Ratio", "42.1 dB"),
            new ("Sampling Variance", "0.0042")
        };
    }

    protected override void OnDisposed()
    {
        AnalyticalMetrics.Clear();
    }
}
