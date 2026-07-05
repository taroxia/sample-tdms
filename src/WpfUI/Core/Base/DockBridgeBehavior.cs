// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout;
using Microsoft.Xaml.Behaviors;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Core.Base;

public sealed class DockBridgeBehavior : Behavior<DockingManager>
{
    private DisposableBag _disposables = new();

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject.IsLoaded)
        {
            StartMonitoring();
        }
        else
        {
            // Loadedイベントを安全にハンドル
            RoutedEventHandler? loadedHandler = null;
            loadedHandler = (s, e) =>
            {
                AssociatedObject.Loaded -= loadedHandler;
                StartMonitoring();
            };
            AssociatedObject.Loaded += loadedHandler;
        }
    }

    private void StartMonitoring()
    {
        _disposables.Dispose(); // 既存の購読をクリア
        _disposables = new DisposableBag();

        // 1. Navigation.Documents コレクションの変更(追加/削除)を監視
        // これにより、Navigatorが切り替わってドキュメント数や中身が変わった瞬間にも即座に同期が走ります
        if (AssociatedObject.DocumentsSource is INotifyCollectionChanged collectionChanged)
        {
            Observable.Create<Unit>(observer =>
            {
                NotifyCollectionChangedEventHandler handler = (s, e) => observer.OnNext(Unit.Default);
                collectionChanged.CollectionChanged += handler;
                return Disposable.Create(() => collectionChanged.CollectionChanged -= handler);
            })
            .ObserveOnCurrentDispatcher()
            .Subscribe(_ => SynchronizeFloatingState())
            .AddTo(ref _disposables);
        }

        // 2. AvalonDock のレイアウト構造そのものが再構築されたイベント (LayoutChanged)
        Observable.Create<Unit>(observer =>
        {
            EventHandler handler = (s, e) => observer.OnNext(Unit.Default);
            AssociatedObject.LayoutChanged += handler;
            return Disposable.Create(() => AssociatedObject.LayoutChanged -= handler);
        })
        .ObserveOnCurrentDispatcher()
        .Subscribe(_ => SynchronizeFloatingState())
        .AddTo(ref _disposables);

        // 3. 【解決策の核心】WPF標準の LayoutUpdated イベントの活用
        // Tear off（ウィンドウ引きはがし）が行われると、WPFの配置・レイアウト計算が必ず走るため、
        // このイベントをR3の「Chunk（またはDebounce）」で最適化し、過剰な負荷を抑えつつ確実に状態の変化をハントします。
        Observable.Create<Unit>(observer =>
        {
            EventHandler handler = (s, e) => observer.OnNext(Unit.Default);
            AssociatedObject.LayoutUpdated += handler;
            return Disposable.Create(() => AssociatedObject.LayoutUpdated -= handler);
        })
        // 連続するレイアウト更新を200ミリ秒間隔に間引き、パフォーマンスを最優先にする（R3公式API）
        .Chunk(TimeSpan.FromMilliseconds(200))
        .ObserveOnCurrentDispatcher()
        .Subscribe(_ => SynchronizeFloatingState())
        .AddTo(ref _disposables);

        // 初回の初期同期を実行
        SynchronizeFloatingState();
    }

    private void SynchronizeFloatingState()
    {
        if (AssociatedObject?.Layout == null) return;

        // Layout.Descendents() および OfType<LayoutDocument>() はDirkster99/AvalonDockに実在する確実な走査アルゴリズムです
        var documents = AssociatedObject.Layout.Descendents().OfType<LayoutDocument>();

        foreach (var doc in documents)
        {
            // doc.Content には DocumentsSource から渡された ViewModel 実体（型ベース）が格納されています
            if (doc.Content is DocumentViewModelBase viewModel)
            {
                // WPFのUIスレッドセーフティを担保しながら、R3のReactivePropertyを安全に同期
                if (viewModel.IsFloating.Value != doc.IsFloating)
                {
                    viewModel.IsFloating.Value = doc.IsFloating;
                }
            }
        }
    }

    protected override void OnDetaching()
    {
        _disposables.Dispose();
        base.OnDetaching();
    }
}
