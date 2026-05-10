// ────────────────────────────────
//
// ────────────────────────────────

using System.Reactive.Disposables;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfUI.Core.Base;

public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    public readonly CompositeDisposable _disposables = new();

    public virtual void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
