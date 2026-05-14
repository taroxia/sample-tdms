// ────────────────────────────────
//
// ────────────────────────────────

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace WpfUI.Core.Base;

public record NavigationItem(
    string Title, string IconKey,
    Type ViewType,
    Type ViewModelType,
    Type? ExplorerViewType,
    Type? ExplorerViewModelType,
    INavigationService NavService)
{
    public ReadOnlyReactivePropertySlim<bool> IsActive { get; } = NavService.CurrentView
        .Select(v => v?.GetType() == ViewType)
        .ToReadOnlyReactivePropertySlim(false);
    //public ReactivePropertySlim<bool> IsActive { get; } = new(false);
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    public IReadOnlyList<NavigationItem> Items { get; }

    private readonly ReactiveProperty<object?> _currentView = new();
    public IReadOnlyReactiveProperty<object?> CurrentView => _currentView;

    private readonly ReactiveProperty<object?> _currentExplorerView = new();
    public IReadOnlyReactiveProperty<object?> CurrentExplorerView => _currentExplorerView;

    private readonly ReactiveProperty<NavigationItem?> _selectedItem = new();
    public IReadOnlyReactiveProperty<NavigationItem?> SelectedItem => _selectedItem;

    public ReactivePropertySlim<bool> IsSidebarExpanded { get; } = new(true);
    public ReactivePropertySlim<bool> IsExplorerExpanded { get; } = new(true);

    public NavigationService(IServiceProvider provider, IEnumerable<NavigationData> data)
    {
        _provider = provider;

        Items = data.Select(x => new NavigationItem(
                    x.Title, x.IconKey,
                    x.ViewType,
                    x.ViewModelType,
                    x.ExplorerViewType!,
                    x.ExplorerViewModelType!, this))
                    .ToList();

        _selectedItem
            .Pairwise()
            .Subscribe(pair =>
            {
                // 古い View 側の DataContext (ViewModel) を Dispose する
                if (_currentView.Value is FrameworkElement oldView)
                {
                    (oldView.DataContext as IDisposable)?.Dispose();
                    oldView.DataContext = null; // 参照を切る
                }
                if (_currentExplorerView.Value is FrameworkElement oldExplorerView)
                {
                    (oldExplorerView.DataContext as IDisposable)?.Dispose();
                    oldExplorerView.DataContext = null; // 参照を切る
                }

                // 新しい View/ViewModel の生成と紐付け
                if (pair.NewItem is not null)
                {
                    var newView = (FrameworkElement)_provider.GetRequiredService(pair.NewItem.ViewType);
                    newView.DataContext = _provider.GetRequiredService(pair.NewItem.ViewModelType);
                    _currentView.Value = newView;
                }
                if (pair.NewItem is not null
                    && pair.NewItem.ExplorerViewType is not null
                    && pair.NewItem.ExplorerViewModelType is not null)
                {
                    var newView = (FrameworkElement)_provider.GetRequiredService(pair.NewItem.ExplorerViewType);
                    newView.DataContext = _provider.GetRequiredService(pair.NewItem.ExplorerViewModelType);
                    _currentExplorerView.Value = newView;
                }
                else { _currentExplorerView.Value = null; }
            })
            //.AddTo(_disposables)  // Singleton.
            ;

        if (Items.Any()) NavigateTo(Items.First());
    }

    public void NavigateTo(NavigationItem Item)
    {
        if (_selectedItem.Value != Item)
        {
            _selectedItem.Value = Item;
        }
    }
}
