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

public record NavigationItem(Type ViewType, Type ViewModelType, string Title, INavigationService NavService)
{
    public string Title { get; } = Title;
    public string Icon { get; init; } = string.Empty;
    public Type ViewType { get; } = ViewType;
    public Type ViewModelType { get; } = ViewModelType;
    //public ReadOnlyReactivePropertySlim<bool> IsActive { get; } = NavService.CurrentView
    //    .Select(v => v?.GetType() == ViewType)
    //    .ToReadOnlyReactivePropertySlim();
    public ReactivePropertySlim<bool> IsActive { get; } = new(false);
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    public IReadOnlyList<NavigationItem> Items { get; }

    private readonly ReactiveProperty<object?> _currentView = new();
    public IReadOnlyReactiveProperty<object?> CurrentView => _currentView;
    private readonly ReactiveProperty<NavigationItem?> _selectedItem = new();
    public IReadOnlyReactiveProperty<NavigationItem?> SelectedItem => _selectedItem;


    public NavigationService(IServiceProvider provider, IEnumerable<NavigationData> data)
    {
        _provider = provider;

        Items = data.Select(x => new NavigationItem(x.ViewType, x.ViewModelType, x.Title, this))
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

                // 新しい View/ViewModel の生成と紐付け
                if (pair.NewItem is not null)
                {
                    var newView = (FrameworkElement)_provider.GetRequiredService(pair.NewItem.ViewType);
                    newView.DataContext = _provider.GetRequiredService(pair.NewItem.ViewModelType);
                    _currentView.Value = newView;
                }
            })
            //.AddTo(_disposables)  // Singleton.
            ;

        if (Items.Any()) NavigateTo(Items.First());
    }

    public void NavigateTo(NavigationItem Item)
    {
        if (_selectedItem.Value == Item) return;

        _selectedItem.Value = Item;
        //_currentView.Value = _provider.GetRequiredService(_selectedItem.Value.ViewType);
        //var viewModel = _provider.GetRequiredService(_selectedItem.Value.ViewModelType);
        //((FrameworkElement)_currentView.Value).DataContext = viewModel;
    }
}
