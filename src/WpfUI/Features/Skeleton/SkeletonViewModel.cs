// ────────────────────────────────
//
// ────────────────────────────────

using System;
using CommunityToolkit.Mvvm.ComponentModel;
// ---.
using WpfUI.Core.Base;
using WpfUI.Features.Skeleton.Documents;
using WpfUI.Features.Skeleton.Explorer;

namespace WpfUI.Features.Skeleton;

public sealed class SkeletonViewModel : FeatureViewModelBase
{
    public SkeletonExpViewModel ExplorerViewModel { get; }
    public SkeletonDocViewModel DocumentsViewModel { get; }

    public SkeletonViewModel(SkeletonService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        // 各サブDI要素へステートシングルトンサービスを注入してインスタンス化
        ExplorerViewModel = new SkeletonExpViewModel(service);
        DocumentsViewModel = new SkeletonDocViewModel(service);
    }

    public void Dispose()
    {
        ExplorerViewModel.Dispose();
        DocumentsViewModel.Dispose();
    }
}
