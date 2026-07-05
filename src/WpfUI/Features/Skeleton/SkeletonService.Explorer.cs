// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using R3;

namespace WpfUI.Features.Skeleton;

public sealed partial class SkeletonService
{
    // Explorerで選択されたチャンネルメタデータを保持するリアクティブプロパティ
    private readonly BindableReactiveProperty<string?> _selectedChannel = new(null);
    public BindableReactiveProperty<string?> SelectedChannel => _selectedChannel;

    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializeExplorerPipeline()
    {
        // 必要に応じた初期化ロジック
    }
    private void ExplorerDispose()
    {
    }

    // ----------------------------------------------------------------
    // Public Logic Methods
    // ----------------------------------------------------------------

    public void SelectChannel(string? channelName)
    {
        _selectedChannel.Value = channelName;
    }
}
