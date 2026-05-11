// ────────────────────────────────
//
// ────────────────────────────────

using Reactive.Bindings;

namespace WpfUI.Core.Base;

public interface INavigationService
{
    IReadOnlyList<NavigationItem> Items { get; }
    IReadOnlyReactiveProperty<object?> CurrentView { get; }
    IReadOnlyReactiveProperty<NavigationItem?> SelectedItem { get; }
    ReactivePropertySlim<bool> IsSidebarExpanded { get; }

    void NavigateTo(NavigationItem item);
}
