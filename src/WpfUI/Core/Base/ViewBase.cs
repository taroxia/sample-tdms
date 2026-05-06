using System.Windows.Controls;

namespace WpfUI.Core.Base;

public abstract class ViewBase<TViewModel> : UserControl where TViewModel : class
{
    public TViewModel? ViewModel => DataContext as TViewModel;

    protected ViewBase()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is not TViewModel vm) return;
            OnViewModelAttached(vm);
        };

        // ViewがロードされたタイミングでViewModelとの同期を行う
        //Loaded += (_, _) => OnViewModelAttached(ViewModel);
    }

    protected virtual void OnViewModelAttached(TViewModel? vm) { }
}
