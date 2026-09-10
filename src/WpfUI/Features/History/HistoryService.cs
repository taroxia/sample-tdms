using System;
using WpfUI.Core.Base;

namespace WpfUI.Features.History;

public sealed partial class HistoryService : BaseService
{
    public HistoryService()
    {
        InitializePipeline();
        InitializeExplorerPipeline();
        InitializeDocumentsPipeline();
    }

    private void InitializePipeline()
    {
        // 全体横断のリアクティブストリーム結合ロジックをここに集約可能
    }

    protected override void OnDisposed()
    {
        ExplorerDispose();
        DocumentsDispose();
    }
}