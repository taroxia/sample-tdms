// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections.ObjectModel;
using R3;

namespace WpfUI.Core.Base;

public interface INavigationService
{
    IReadOnlyList<NavigationItem> Items { get; }
    BindableReactiveProperty<object?> CurrentView { get; }
    BindableReactiveProperty<object?> CurrentExplorerView { get; }
    BindableReactiveProperty<object?> CurrentDocumentView { get; }
    ReactiveProperty<NavigationItem?> SelectedItem { get; }
    ReactiveProperty<bool> IsSidebarExpanded { get; }
    ReactiveProperty<bool> IsExplorerExpanded { get; }

    ObservableCollection<DocumentViewModelBase> Documents { get; }

    void NavigateTo(NavigationItem item);

    void CloseDocument(DocumentViewModelBase document);
}
