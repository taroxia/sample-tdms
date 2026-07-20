// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Windows.Controls;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Documents;

public partial class SkeletonDocView : ViewBase<SkeletonDocViewModel>
{
    public SkeletonDocView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(SkeletonDocViewModel? viewModel)
    {
        if (viewModel is null) return;
        DataContext = viewModel;
    }
}
