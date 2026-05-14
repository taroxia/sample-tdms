// ────────────────────────────────
//
// ────────────────────────────────

using Reactive.Bindings;

namespace WpfUI.Core.Base;

public interface INavigationService
{
    IReadOnlyList<NavigationItem> Items { get; }
    IReadOnlyReactiveProperty<object?> CurrentView { get; }
    IReadOnlyReactiveProperty<object?> CurrentExplorerView { get; }
    IReadOnlyReactiveProperty<NavigationItem?> SelectedItem { get; }
    ReactivePropertySlim<bool> IsSidebarExpanded { get; }
    ReactivePropertySlim<bool> IsExplorerExpanded { get; }

    void NavigateTo(NavigationItem item);
}
