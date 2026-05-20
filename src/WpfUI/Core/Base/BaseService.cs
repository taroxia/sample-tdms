// ────────────────────────────────
//
// ────────────────────────────────

using CommunityToolkit.Mvvm.ComponentModel;
using R3;

namespace WpfUI.Core.Base;

public abstract class BaseService : IDisposable
{
    protected DisposableBag _disposables = new();

    public virtual void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
