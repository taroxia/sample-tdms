using System;
using WpfUI.Core.Base;

namespace WpfUI.Features.Measure;

public sealed partial class MeasureService : BaseService
{
    public MeasureService()
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