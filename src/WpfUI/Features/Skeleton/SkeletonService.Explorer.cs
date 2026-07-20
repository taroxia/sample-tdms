using System;
using R3;

namespace WpfUI.Features.Skeleton;

public sealed partial class SkeletonService
{
    public BindableReactiveProperty<string?> SelectedNode { get; } = new(null);

    private void InitializeExplorerPipeline()
    {
        // Explorerでの選択を自動的にDocuments側にブリッジする例（最強のリアクティブ同期）
        SelectedNode
            .Subscribe(node => 
            {
                if (node is not null)
                {
                    UpdateTargetContent($"Auto-Routed: {node}");
                }
            })
            .AddTo(ref _disposables); // 必要に応じて破棄用の管理へ追加
    }

    private void ExplorerDispose()
    {
        SelectedNode.Dispose();
    }
}