// ────────────────────────────────
//
// ────────────────────────────────

using CommunityToolkit.Mvvm.ComponentModel;
using R3;

namespace WpfUI.Core.Base;

public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    protected DisposableBag _disposables = new();
    private readonly Subject<Unit> _onDisposed = new();
    public Observable<Unit> Disposed => _onDisposed;
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed) return;

        _onDisposed.OnNext(Unit.Default);
        _onDisposed.OnCompleted();
        _onDisposed.Dispose();

        OnDisposed();

        _disposables.Dispose();

        _isDisposed = true;

        GC.SuppressFinalize(this);
    }

    protected virtual void OnDisposed() { }
}

public abstract class ExplorerViewModelBase : ViewModelBase { }

public abstract class FeatureViewModelBase : ViewModelBase { }

public abstract class DocumentViewModelBase : ViewModelBase
{
    // AvalonDockの各レイアウトアイテムと双方向同期するためのR3プロパティ群
    public ReactiveProperty<string> Title { get; }
    public ReactiveProperty<bool> IsSelected { get; }
    public ReactiveProperty<bool> IsActive { get; }
    public ReactiveProperty<bool> IsFloating { get; }
    public ReactiveProperty<string> ContentId { get; }

    protected DocumentViewModelBase(string title, string contentId)
    {
        Title = new ReactiveProperty<string>(title).AddTo(ref _disposables);
        IsSelected = new ReactiveProperty<bool>(false).AddTo(ref _disposables);
        IsActive = new ReactiveProperty<bool>(false).AddTo(ref _disposables);
        IsFloating = new ReactiveProperty<bool>(false).AddTo(ref _disposables);
        ContentId = new ReactiveProperty<string>(Guid.NewGuid().ToString()).AddTo(ref _disposables);
    }
}


