// ────────────────────────────────
//
// ────────────────────────────────

using CommunityToolkit.Mvvm.ComponentModel;
using R3;

namespace WpfUI.Core.Base;

public abstract partial class BaseService : IDisposable
{
    public readonly CompositeDisposable _disposables = new();

    public virtual void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
