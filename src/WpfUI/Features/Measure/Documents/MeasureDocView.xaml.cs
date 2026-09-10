// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Windows.Controls;
using WpfUI.Core.Base;

namespace WpfUI.Features.Measure.Documents;

public partial class MeasureDocView : ViewBase<MeasureDocViewModel>
{
    public MeasureDocView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(MeasureDocViewModel? viewModel)
    {
        if (viewModel is null) return;
        DataContext = viewModel;
    }
}
