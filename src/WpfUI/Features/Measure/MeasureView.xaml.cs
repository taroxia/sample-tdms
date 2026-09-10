using WpfUI.Core.Base;

namespace WpfUI.Features.Measure;

public partial class MeasureView : ViewBase<MeasureViewModel>
{
    public MeasureView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(MeasureViewModel? viewModel)
    {
        if (viewModel is null) return;
        this.DataContext = viewModel;
    }
}