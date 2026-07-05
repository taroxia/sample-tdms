// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Shell;

public partial class MainViewModel : ViewModelBase
{
    public INavigationService Navigation { get; }

    // --- State Properties ---
    public BindableReactiveProperty<object?> CurrentView { get; }
    public BindableReactiveProperty<object?> CurrentExplorerView { get; }
    public BindableReactiveProperty<object?> CurrentDocumentView { get; }
    public BindableReactiveProperty<bool> HasExplorer { get; }
    public BindableReactiveProperty<bool> HasDocument { get; }

    public ObservableCollection<DocumentViewModelBase> BoundDocuments => Navigation.Documents;

    // アプリケーション名（タイトルバー用）
    public BindableReactiveProperty<string> Title { get; } = new("TDMS Analysis Dashboard");

    // --- Commands ---
    public ReactiveCommand<NavigationItem> NavigateCommand { get; } = new();
    public ReactiveCommand ToggleSidebarCommand { get; } = new();
    public ReactiveCommand ToggleExplorerCommand { get; } = new();
    public ReactiveCommand<DocumentViewModelBase> CloseDocumentCommand { get; } = new();

    public MainViewModel(INavigationService navigation)
    {
        Navigation = navigation;

        // Property.
        CurrentView = navigation.CurrentView
            .ToBindableReactiveProperty()
            .AddTo(ref _disposables);

        CurrentExplorerView = navigation.CurrentExplorerView
            .ToBindableReactiveProperty()
            .AddTo(ref _disposables);

        CurrentDocumentView = navigation.CurrentDocumentView
            .ToBindableReactiveProperty()
            .AddTo(ref _disposables);

        HasExplorer = navigation.CurrentExplorerView
                .Select(view => view is not null)
                .ToBindableReactiveProperty()
                .AddTo(ref _disposables);

        HasDocument = navigation.CurrentDocumentView
                .Select(view => view is not null)
                .ToBindableReactiveProperty()
                .AddTo(ref _disposables);

        // Command.
        NavigateCommand
            .Subscribe(x => navigation.NavigateTo(x))
            .AddTo(ref _disposables);

        ToggleSidebarCommand
            .Subscribe(_ =>
            navigation.IsSidebarExpanded.Value = !navigation.IsSidebarExpanded.Value
        ).AddTo(ref _disposables);


        ToggleExplorerCommand
            .Subscribe(_ =>
            navigation.IsExplorerExpanded.Value = !navigation.IsExplorerExpanded.Value
        ).AddTo(ref _disposables);

        CloseDocumentCommand
            .Subscribe(doc =>
            Navigation.CloseDocument(doc)
        ).AddTo(ref _disposables);
    }
}
