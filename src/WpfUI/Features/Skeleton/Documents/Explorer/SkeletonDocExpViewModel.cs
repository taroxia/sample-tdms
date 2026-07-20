// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Documents.Explorer;

public sealed class SkeletonDocExpViewModel : ExplorerViewModelBase
{
    public List<string> NodeList { get; } = ["Stream_Alpha_01", "Stream_Beta_02", "Stream_Gamma_03"];
    public BindableReactiveProperty<string?> SelectedNode { get; }

    public SkeletonDocExpViewModel(SkeletonService service)
    {
        SelectedNode = new BindableReactiveProperty<string?>(null).AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
        SelectedNode.Dispose();
    }
}
