// ────────────────────────────────
//
// ────────────────────────────────

using R3;

namespace WpfUI.Core.Base;

public interface INavigationService
{
    IReadOnlyList<NavigationItem> Items { get; }
    BindableReactiveProperty<object?> CurrentView { get; }
    BindableReactiveProperty<object?> CurrentExplorerView { get; }
    ReactiveProperty<NavigationItem?> SelectedItem { get; }
    ReactiveProperty<bool> IsSidebarExpanded { get; }
    ReactiveProperty<bool> IsExplorerExpanded { get; }

    void NavigateTo(NavigationItem item);
}
