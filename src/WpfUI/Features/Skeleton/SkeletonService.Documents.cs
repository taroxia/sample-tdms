// ────────────────────────────────
//
// ────────────────────────────────

using System;
using R3;

namespace WpfUI.Features.Skeleton;

public sealed partial class SkeletonService
{
    public BindableReactiveProperty<string?> SharedTargetContent { get; } = new(null);

    // Explorer部の横幅ピクセル状態（初期値 260.0）
    public BindableReactiveProperty<double> ExplorerWidth { get; } = new(260.0);

    // Explorer部の開閉状態
    public BindableReactiveProperty<bool> IsExplorerExpanded { get; } = new(true);

    private void InitializeDocumentsPipeline()
    {
    }

    private void DocumentsDispose()
    {
        SharedTargetContent.Dispose();
        ExplorerWidth.Dispose();
        IsExplorerExpanded.Dispose();
    }

    public void UpdateTargetContent(string? content)
    {
        SharedTargetContent.Value = content;
    }
}
