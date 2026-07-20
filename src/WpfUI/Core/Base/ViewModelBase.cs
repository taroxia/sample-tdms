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
    public ReactiveProperty<bool> IsActive { get; }
    public ReactiveProperty<bool> IsFloating { get; }
    public ReactiveProperty<bool> IsSelected { get; }
    public ReactiveProperty<string> ContentId { get; }
    public ReactiveProperty<Type?> CurrentContextKey { get; }
    public ReactiveProperty<string> Title { get; }

    protected DocumentViewModelBase(string title, string contentId)
    {
        IsActive = new ReactiveProperty<bool>(false).AddTo(ref _disposables);
        IsFloating = new ReactiveProperty<bool>(false).AddTo(ref _disposables);
        IsSelected = new ReactiveProperty<bool>(false).AddTo(ref _disposables);
        ContentId = new ReactiveProperty<string>(Guid.NewGuid().ToString()).AddTo(ref _disposables);
        CurrentContextKey = new ReactiveProperty<Type?>().AddTo(ref _disposables);
        Title = new ReactiveProperty<string>(title).AddTo(ref _disposables);

        //IsFloating
        //    .Where(x => x)
        //    .Subscribe(_ => IsInteracted.Value = true)
        //    .AddTo(ref _disposables);
    }
}


