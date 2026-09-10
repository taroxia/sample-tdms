using System;
using R3;

namespace WpfUI.Features.History;

public sealed partial class HistoryService
{
    // WPFのBindingシステムと完全結合可能なR3標準プロパティ
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