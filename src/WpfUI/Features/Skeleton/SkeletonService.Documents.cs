// ────────────────────────────────
//
// ────────────────────────────────

using System;
using R3;

namespace WpfUI.Features.Skeleton;

public sealed partial class SkeletonService
{
    // 多段配置や軸割り当ての状態トポロジーを管理するプロパティ群
    private readonly ReactiveProperty<int> _plotLayersCount = new(1);
    public ReadOnlyReactiveProperty<int> PlotLayersCount => _plotLayersCount;

    private readonly ReactiveProperty<string?> _xAxisChannel = new(null);
    public ReadOnlyReactiveProperty<string?> XAxisChannel => _xAxisChannel;

    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializeDocumentsPipeline()
    {
    }
    private void DocumentsDispose()
    {
    }

    // ----------------------------------------------------------------
    // Public Logic Methods
    // ----------------------------------------------------------------

    public void AssignXAxis(string? channelName)
    {
        _xAxisChannel.Value = channelName;
    }

    public void AddPlotLayer()
    {
        _plotLayersCount.Value++;
    }

    public void ClearLayout()
    {
        _xAxisChannel.Value = null;
        _plotLayersCount.Value = 1;
    }

    // ----------------------------------------------------------------
    // Private Helper Methods
    // ----------------------------------------------------------------
}
