// ────────────────────────────────
//
// ────────────────────────────────

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
    public ReactiveProperty<NavigationItem?> SelectedItem { get; } = new();

    public ReactiveProperty<bool> IsSidebarExpanded { get; } = new(true);
    public ReactiveProperty<bool> IsExplorerExpanded { get; } = new(true);

    public NavigationService(IServiceProvider provider, IEnumerable<NavigationData> data)
    {
        _provider = provider;

        Items = data.Select(x =>
            new NavigationItem(
                x.Title, x.IconKey,
                x.ViewType,
                x.ViewModelType,
                x.ExplorerViewType!,
                x.ExplorerViewModelType!,
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

        if (pair.Current is null)
        {
            CurrentView.Value = null;
            CurrentExplorerView.Value = null;
            return;
        }

        var view = (FrameworkElement)_provider.GetRequiredService(pair.Current.ViewType);
        view.DataContext = _provider.GetRequiredService(pair.Current.ViewModelType);
        CurrentView.Value = view;

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

    public override void Dispose()
    {
        foreach (var item in Items)
        {
            item.Dispose();
        }
        base.Dispose();
    }
}
