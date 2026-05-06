using Reactive.Bindings;

namespace WpfUI.Core.Base;

public enum ViewType
{
    DataExplorer,
    LiveAnalytics,
    Inspector,
    Settings
}

public interface INavigationService
{
    // 現在表示中の View (Object) を保持するリアクティブなプロパティ
    IReadOnlyReactiveProperty<object?> CurrentView { get; }
    void NavigateTo<T>() where T : class;
    void NavigateTo(ViewType viewType);
}
