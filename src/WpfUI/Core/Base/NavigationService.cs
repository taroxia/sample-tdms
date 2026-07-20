// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using R3;
using Windows.UI.Text;

namespace WpfUI.Core.Base;

public record NavigationItem(
    string Title, string IconKey,
    Type ViewType,
    Type ViewModelType,
    Type? ExplorerViewType,
    Type? ExplorerViewModelType,
    Type? DocumentViewType,
    Type? DocumentViewModelType,
    BindableReactiveProperty<object?> CurrentView) : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    public void Dispose() => _disposables.Dispose();

    public BindableReactiveProperty<bool> IsActive { get; } = CurrentView
        .Select(v => v?.GetType() == ViewType)
        .ToBindableReactiveProperty(false);
}

public sealed class NavigationService : BaseService, INavigationService
{
    private readonly IServiceProvider _provider;
    public IReadOnlyList<NavigationItem> Items { get; }

    public BindableReactiveProperty<object?> CurrentView { get; } = new();
    public BindableReactiveProperty<object?> CurrentExplorerView { get; } = new();
    public BindableReactiveProperty<object?> CurrentDocumentView { get; } = new();
    public ReactiveProperty<NavigationItem?> SelectedItem { get; } = new();

    public ReactiveProperty<bool> IsSidebarExpanded { get; } = new(true);
    public ReactiveProperty<bool> IsExplorerExpanded { get; } = new(true);

    public BindableReactiveProperty<Type?> CurrentActiveViewModelType { get; }
    public ObservableCollection<DocumentViewModelBase> Documents { get; } = new();

    private readonly Dictionary<Type, List<DocumentViewModelBase>> _dockedDocumentsPool = new();

    public NavigationService(IServiceProvider provider, IEnumerable<NavigationData> data)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;

        Items = data.Select(x =>
            new NavigationItem(
                x.Title, x.IconKey,
                x.ViewType,
                x.ViewModelType,
                x.ExplorerViewType,
                x.ExplorerViewModelType,
                x.DocumentViewType,
                x.DocumentViewModelType,
                CurrentView))
             .ToList();

        CurrentActiveViewModelType = SelectedItem
            .Select(item => item?.ViewModelType)
            .ToBindableReactiveProperty();

        SelectedItem
            .Pairwise()
            .Subscribe(OnNavigationChanged)
            .AddTo(ref _disposables);

        // Default Select.
        if (Items.Any()) NavigateTo(Items.First());
    }

    private void OnNavigationChanged((NavigationItem? Previous, NavigationItem? Current) pair)
    {
        if (pair.Previous is not null && pair.Previous.DocumentViewModelType is not null)
        {
            var previousVmType = pair.Previous.ViewModelType;
            var previousDocVmType = pair.Previous.DocumentViewModelType;

            // 旧画面のコンテキストキーを持ち、かつ「現在ドッキング中（非フローティング）」のドキュメントを抽出
            var targets = Documents
                .Where(d => d.CurrentContextKey.Value == previousVmType && !d.IsFloating.Value)
                .ToList();

            if (!_dockedDocumentsPool.TryGetValue(previousVmType, out var poolList))
            {
                poolList = new List<DocumentViewModelBase>();
                _dockedDocumentsPool[previousVmType] = poolList;
            }
            poolList.Clear();

            foreach (var doc in targets)
            {
                // UIコレクションから一旦除外
                Documents.Remove(doc);

                // 正規のデフォルトドキュメント型であれば、プールに記憶せずそのまま使い捨て（解放）
                if (doc.GetType() == previousDocVmType)
                {
                    continue;
                }

                // 再ドッキングによって混入していた「正規以外」のドキュメントのみプールへ退避
                poolList.Add(doc);
            }
        }

        DisposeView(CurrentView.Value);
        DisposeView(CurrentExplorerView.Value);

        CurrentDocumentView.Value = null;

        if (pair.Current is null)
        {
            CurrentView.Value = null;
            CurrentExplorerView.Value = null;
            return;
        }

        // 1. Main View.
        var view = (FrameworkElement)_provider.GetRequiredService(pair.Current.ViewType);
        view.DataContext = _provider.GetRequiredService(pair.Current.ViewModelType);
        CurrentView.Value = view;

        // 2. Explorer View.
        if (pair.Current.ExplorerViewType is not null)
        {
            var explorer = (FrameworkElement)_provider.GetRequiredService(pair.Current.ExplorerViewType);
            explorer.DataContext = _provider.GetRequiredService(pair.Current.ExplorerViewModelType!);
            CurrentExplorerView.Value = explorer;
        }
        else
        {
            CurrentExplorerView.Value = null;
        }

        // 3. Document View.
        if (pair.Current.DocumentViewModelType is not null)
        {
            var currentVmType = pair.Current.ViewModelType;
            var currentDocVmType = pair.Current.DocumentViewModelType;
            DocumentViewModelBase? defaultDoc = null;

            // ① すでに Documents 内に存在するか確認（Tear off された状態で残留している場合など）
            //defaultDoc = Documents.FirstOrDefault(d => d.GetType() == pair.Current.DocumentViewModelType && d.CurrentContextKey.Value == currentVmType);
            defaultDoc = Documents.FirstOrDefault(d => d.GetType() == pair.Current.DocumentViewModelType);


            bool hasDocViewModel = false;
            if (pair.Current?.DocumentViewModelType is { } targetType)
            {
                hasDocViewModel = _dockedDocumentsPool.Values
                               .SelectMany(list => list)
                               .Any(vm => vm != null && targetType.IsAssignableFrom(vm.GetType()))
                               || Documents.Any(d => d.GetType() == targetType && d.CurrentContextKey.Value == currentVmType);
            }

            // ③ プールにも既存コレクションにも無ければ、DIコンテナから正規ドキュメントを新規 Transient 生成
            if (defaultDoc is null && !hasDocViewModel)
            {

                // A. 正規のドキュメントは指定通り Transient として毎回新規生成
                defaultDoc = (DocumentViewModelBase)_provider.GetRequiredService(currentDocVmType);

                SetupTearOffTracking(defaultDoc);

                defaultDoc.CurrentContextKey.Value = currentVmType;
                Documents.Add(defaultDoc);
            }

            // B. 退避プールに、この画面宛ての「正規以外のドキュメント（異物）」が眠っていれば復元
            if (_dockedDocumentsPool.TryGetValue(currentVmType, out var poolList) && poolList.Any())
            {
                foreach (var doc in poolList)
                {
                    Documents.Add(doc);
                }
                poolList.Clear();
            }

            // アクティブ化
            if (defaultDoc is not null)
            {
                defaultDoc.IsSelected.Value = true;
                defaultDoc.IsActive.Value = true;
            }
            CurrentDocumentView.Value = defaultDoc;
        }
    }

    private void SetupTearOffTracking(DocumentViewModelBase doc)
    {
        // IsFloating が True かつ CurrentActiveViewModelType が変化した時のみコンテキストを書き換える
        doc.IsFloating
            .CombineLatest(CurrentActiveViewModelType, (isFloating, activeType) => (isFloating, activeType))
            .Subscribe(state =>
            {
                if (state.isFloating && state.activeType is not null)
                {
                    doc.CurrentContextKey.Value = state.activeType;
                }
            })
            .AddTo(ref _disposables); // サービス終了時に一括解放
    }

    public void CloseDocument(DocumentViewModelBase document)
    {
        if (document == null) return;

        // コレクションから除外することでView（AvalonDock）から消し去る
        if (Documents.Remove(document))
        {
            // Transientのライフサイクルを安全に終了させ、確実にメモリとストリームを解放する
            document.Dispose();
        }
        foreach (var kp in _dockedDocumentsPool)
        {
            if (kp.Value.Remove(document))
            {
                document.Dispose();
                break;
            }
        }
    }

    private static void DisposeView(object? viewObj)
    {
        if (viewObj is FrameworkElement view)
        {
            (view.DataContext as IDisposable)?.Dispose();
            view.DataContext = null;
        }
    }

    public void NavigateTo(NavigationItem Item)
    {
        if (SelectedItem.Value != Item)
        {
            SelectedItem.Value = Item;
        }
    }

    protected override void OnDisposed()
    {
        foreach (var item in Items)
        {
            item.Dispose();
        }
        foreach (var doc in Documents)
        {
            doc.Dispose();
        }
        foreach (var list in _dockedDocumentsPool.Values)
        {
            foreach (var doc in list)
            {
                doc.Dispose();
            }
        }
        _dockedDocumentsPool.Clear();
        Documents.Clear();
    }
}
