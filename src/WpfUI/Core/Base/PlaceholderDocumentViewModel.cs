using System;
using R3;

namespace WpfUI.Core.Base;

/// <summary>
/// 他の機能のドッキングビューを受け入れるために、ドメインコンテキストを維持したまま常駐する汎用プレースホルダーViewModel
/// </summary>
public sealed class PlaceholderDocumentViewModel : DocumentViewModelBase
{
    public PlaceholderDocumentViewModel(Type targetContextKey, string parentTitle) 
        : base($"📊 {parentTitle} (ドッキング領域)", "Placeholder_" + Guid.NewGuid().ToString("N"))
    {
        CurrentContextKey.Value = targetContextKey ?? throw new ArgumentNullException(nameof(targetContextKey));
        IsFloating.Value = false;
    }
}
