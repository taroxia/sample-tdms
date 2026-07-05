// ────────────────────────────────
//
// ────────────────────────────────

using System;
using R3;
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
    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializePipeline()
    {
    }
    protected override void OnDisposed()
    {
        ExplorerDispose();
        DocumentsDispose();
    }
}
