// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using R3;

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

    public ObservableCollection<DocumentViewModelBase> Documents { get; } = new();

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

        SelectedItem
            .Pairwise()
            .Subscribe(OnNavigationChanged)
            .AddTo(ref _disposables);

        if (Items.Any()) NavigateTo(Items.First());
    }

    private void OnNavigationChanged((NavigationItem? Previous, NavigationItem? Current) pair)
    {
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
        if (pair.Current.DocumentViewModelType is null)
        {
            var attachedDocs = Documents.Where(d => !d.IsFloating.Value).ToList();
            foreach (var doc in attachedDocs)
            {
                Documents.Remove(doc);
                doc.Dispose();
            }
        }
        else
        {
            // すでにコレクション内に該当型のViewModelが存在するか確認（型ベース判定）
            var existingDoc = Documents.FirstOrDefault(d => d.GetType() == pair.Current.DocumentViewModelType);

            if (existingDoc != null)
            {
                // すでにTear offされているかメイン内にある場合は、最前面（Selected）にする
                existingDoc.IsSelected.Value = true;
                existingDoc.IsActive.Value = true;
                CurrentDocumentView.Value = existingDoc;
            }
            else
            {
                // 存在しない場合は、DIコンテナから新規にTransientとして生成
                var newDocVm = (DocumentViewModelBase)_provider.GetRequiredService(pair.Current.DocumentViewModelType);

                // 初回追加時にコレクションに永続化
                Documents.Add(newDocVm);
                newDocVm.IsSelected.Value = true;
                newDocVm.IsActive.Value = true;
                CurrentDocumentView.Value = newDocVm;
            }
        }
#if false
        if (pair.Current.DocumentViewType is not null)
        {
            var docView = (FrameworkElement)_provider.GetRequiredService(pair.Current.DocumentViewType);
            docView.DataContext = _provider.GetRequiredService(pair.Current.DocumentViewModelType!);
            CurrentDocumentView.Value = docView;
        }
        else
        {
            CurrentDocumentView.Value = null;
        }
#endif
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
    }
}
