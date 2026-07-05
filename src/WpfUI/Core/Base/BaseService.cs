// ────────────────────────────────
//
// ────────────────────────────────

using CommunityToolkit.Mvvm.ComponentModel;
using R3;

namespace WpfUI.Core.Base;

public abstract class BaseService : IDisposable
{
    protected DisposableBag _disposables = new();
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed) return;

        OnDisposed();

        _disposables.Dispose();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    protected virtual void OnDisposed() { }
}
