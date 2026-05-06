using Microsoft.Extensions.DependencyInjection;
using Reactive.Bindings;

namespace WpfUI.Core.Base;

public sealed class NavigationService(IServiceProvider provider) : INavigationService
{
    private readonly ReactiveProperty<object?> _currentView = new();
    public IReadOnlyReactiveProperty<object?> CurrentView => _currentView;

    public void NavigateTo<T>() where T : class
    {
        // DI コンテナから View を取得して切り替え
        var view = provider.GetRequiredService<T>();
        _currentView.Value = view;
    }
    public void NavigateTo(ViewType viewType)
    {
        // Feature名に基づいた型解決の実装例
        switch (viewType)
        {
            //case ViewType.DataExplorer: NavigateTo<Features.DataExplorer.DataExplorerViewModel>(); break;
            case ViewType.LiveAnalytics: NavigateTo<Features.LiveAnalytics.LiveAnalyticsViewModel>(); break;
            case ViewType.Settings: NavigateTo<Features.Settings.SettingsViewModel>(); break;
            default: throw new ArgumentOutOfRangeException(nameof(viewType), viewType, null);
        }
    }
}
