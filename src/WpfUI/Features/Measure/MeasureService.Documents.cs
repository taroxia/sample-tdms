// ────────────────────────────────
//
// ────────────────────────────────

using System;
using R3;

namespace WpfUI.Features.Measure;

public sealed partial class MeasureService
{
    public BindableReactiveProperty<string?> SharedTargetContent { get; } = new(null);


    private void InitializeDocumentsPipeline()
    {
    }

    private void DocumentsDispose()
    {
        SharedTargetContent.Dispose();
    }

    public void UpdateTargetContent(string? content)
    {
        SharedTargetContent.Value = content;
    }
}
