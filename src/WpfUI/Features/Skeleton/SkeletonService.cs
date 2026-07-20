using System;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton;

public sealed partial class SkeletonService : BaseService
{
    public SkeletonService()
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