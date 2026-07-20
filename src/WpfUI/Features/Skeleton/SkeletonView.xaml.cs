using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton;

public partial class SkeletonView : ViewBase<SkeletonViewModel>
{
    public SkeletonView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(SkeletonViewModel? viewModel)
    {
        if (viewModel is null) return;
        this.DataContext = viewModel;
    }
}