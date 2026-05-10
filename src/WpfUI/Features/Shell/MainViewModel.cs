// ────────────────────────────────
//
// ────────────────────────────────

using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Windows.Services.Maps;
using WpfUI.Core.Base;

namespace WpfUI.Features.Shell;

public partial class MainViewModel : ViewModelBase
{
    public INavigationService Navigation { get; }

    // --- State Properties ---
    public ReadOnlyReactivePropertySlim<object?> CurrentView { get; }

    // アプリケーション名（タイトルバー用）
    public ReactivePropertySlim<string> Title { get; } = new("TDMS Analysis Dashboard");

    // --- Commands ---
    public ReactiveCommandSlim<NavigationItem> NavigateCommand { get; }

    public MainViewModel(INavigationService navigation)
    {
        Navigation = navigation;

        // Service 側の CurrentPage を購読して View に通知
        CurrentView = navigation.CurrentView
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        // 画面遷移コマンドの実装
        NavigateCommand = new ReactiveCommandSlim<NavigationItem>()
            .WithSubscribe(x =>
            {
                navigation.NavigateTo(x);
                foreach (var navItem in Navigation.Items)
                {
                    navItem.IsActive.Value = (navItem == x);
                }
            })
            .AddTo(_disposables);
    }
}
