using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using WpfUI.Core.Base;
using WpfUI.Features.Settings;

namespace WpfUI.Features.Shell;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    // --- State Properties ---
    // NavigationService から現在の ViewModel を受け取るプロパティ
    // これを View の ContentControl に Binding する
    public ReadOnlyReactivePropertySlim<object?> CurrentViewModel { get; }
    //    public ReadOnlyReactivePropertySlim<bool> IsBusy { get; }
    public List<NavigationItem> NavigationItems { get; }

    // アプリケーション名（タイトルバー用）
    public ReactivePropertySlim<string> Title { get; } = new("TDMS Analysis Dashboard");

    // サイドバーの開閉状態などを管理する場合
    //public ReactivePropertySlim<bool> IsNavigationOpen { get; } = new(true);
    // --- Commands ---
    public ReactiveCommandSlim<ViewType> NavigateCommand { get; }

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        // Service 側の CurrentPage を購読して View に通知
        CurrentViewModel = navigationService.CurrentView
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        // PlotValue.Subscribe(v => {
        //     /* ScottPlotの更新処理など */
        // }).AddTo(_disposables);

        NavigationItems =
        [
            //new(typeof(DataExplorerViewModel), "Data Explorer", _navigationService),
            //new(typeof(LiveAnalyticsViewModel), "Live Analytics", _navigationService),
            //new(typeof(InspectorViewModel), "Inspector", _navigationService),
            new(typeof(SettingsViewModel), "Settings", _navigationService)
        ];

        // 3. 画面遷移コマンドの実装
        NavigateCommand = new ReactiveCommandSlim<ViewType>()
            .WithSubscribe(x => _navigationService.NavigateTo(x))
            .AddTo(_disposables);

        // 初期画面への遷移
        _navigationService.NavigateTo<SettingsViewModel>();
    }
}

public record NavigationItem(Type Type, string Label, INavigationService NavService)
{
    public ReadOnlyReactivePropertySlim<bool> IsActive { get; } = NavService.CurrentView
        .Select(v => v?.GetType() == Type)
        .ToReadOnlyReactivePropertySlim();
}

/*
using CommunityToolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using WpfUI.Core.Base;

using WpfUI.Features.Dashboard;
using WpfUI.Features.Settings;

namespace WpfUI.Features.Shell;


public sealed class MainViewModel : ObservableObject
{
    public IReadOnlyReactiveProperty<object?> CurrentView { get; }

    public ReactiveCommand ShowDashboardCommand { get; }
    public ReactiveCommand ShowSettingsCommand { get; }

    public MainViewModel(INavigationService navigationService)
    {
        CurrentView = navigationService.CurrentView;

        ShowDashboardCommand = new ReactiveCommand().WithSubscribe(() => navigationService.NavigateTo<DashboardView>());
        ShowSettingsCommand = new ReactiveCommand().WithSubscribe(() => navigationService.NavigateTo<SettingsView>());

        navigationService.NavigateTo<DashboardView>();
    }




    //public MainViewModel() : this(".NET 9 Designer Mode") { }

    public string Title { get; init; } = ".NET 9 + C# 13 WPF Application";
    public DateTime CurrentTime { get; init; } = DateTime.Now;

    // Feature 固有のロジックをここに集約
}
*/
