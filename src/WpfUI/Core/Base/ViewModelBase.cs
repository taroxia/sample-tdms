using CommunityToolkit.Mvvm.ComponentModel;
using System.Reactive.Disposables;

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
