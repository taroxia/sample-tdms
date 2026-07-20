// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Linq;
using System.Windows;
using AvalonDock;
using AvalonDock.Layout;
using Microsoft.Xaml.Behaviors;
using R3;

namespace WpfUI.Core.Base;

public class DockBridgeBehavior : Behavior<DockingManager>
{
    private DisposableBag _disposables;

    protected override void OnAttached()
    {
        base.OnAttached();

        // メイン固定ペインへの強制ルーティング戦略を設定
        AssociatedObject.LayoutUpdateStrategy = new SafeLayoutUpdateStrategy();

        // LayoutUpdated イベントから R3 の ReactiveProperty へ状態を安全に同期
        // 頻発するレイアウトイベントを等間隔、または適切なタイミングで同期
        Observable.FromEvent<EventHandler, EventArgs>(
            h => (s, e) => h(e),
            h => AssociatedObject.LayoutUpdated += h,
            h => AssociatedObject.LayoutUpdated -= h)

        .Chunk(TimeSpan.FromMilliseconds(100)) // 負荷軽減のための間引き処理
        .ObserveOnCurrentDispatcher()
        .Subscribe(_ => SyncDockState())
        .AddTo(ref _disposables);

        // 2. 初回のアタッチ時に現在のレイアウト状態を一度強制同期
        Dispatcher.BeginInvoke(new Action(SyncDockState));
    }

    protected override void OnDetaching()
    {
        _disposables.Dispose();
        base.OnDetaching();
    }

    private void SyncDockState()
    {
        var layout = AssociatedObject.Layout;
        if (layout == null) return;

        // ツリー上のすべての LayoutDocument を安全に取得
        var layoutDocs = layout.Descendents().OfType<LayoutDocument>().ToList();

        foreach (var layoutDoc in layoutDocs)
        {
            if (layoutDoc.Content is not DocumentViewModelBase vm) continue;

            // 1. 実在するプロパティからフローティング状態を ViewModel へ同期
            if (vm.IsFloating.Value != layoutDoc.IsFloating)
            {
                vm.IsFloating.Value = layoutDoc.IsFloating;
            }

            // 2. ドッキング状態（非フローティング）の場合、再ドッキングによる所属コンテキストの書き換えをチェック

            if (layoutDoc.IsFloating || layoutDoc.Parent is not LayoutDocumentPane parentPane) continue;

            // A) 同一ペイン内に既に存在する、自分以外の有効なドキュメント ViewModel を取得
            var siblingVm = parentPane.Children
                .OfType<LayoutDocument>()
                .Select(d => d.Content as DocumentViewModelBase)
                .FirstOrDefault(s => s != null && s != vm);

            if (siblingVm?.CurrentContextKey.Value is { } siblingKey)
            {
                if (vm.CurrentContextKey.Value != siblingKey)
                {
                    vm.CurrentContextKey.Value = siblingKey;
                }
            }
        }
    }
}

/// <summary>
/// フローティングウィンドウ配下への誤挿入を防ぎ、
/// ドキュメントが常にメイン固定領域のペインに配置されるようルーティングする安全な戦略クラス。
/// </summary>
public sealed class SafeLayoutUpdateStrategy : ILayoutUpdateStrategy
{
    public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
    {
        // フローティング窓配下にない、メイン領域の固定ペイン（かつ PaneContextKey を持つペインなど）を特定
        var mainDocumentPane = layout.Descendents()
            .OfType<LayoutDocumentPane>()
            .FirstOrDefault(p => !IsInsideFloatingWindow(p));

        if (mainDocumentPane != null && destinationContainer != mainDocumentPane)
        {
            mainDocumentPane.Children.Add(anchorableToShow);
            return true;
        }
        return false;
    }

    public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow) { }

    public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer) => false;

    public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow) { }

    private static bool IsInsideFloatingWindow(LayoutElement element)
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current is LayoutFloatingWindow)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }
}
