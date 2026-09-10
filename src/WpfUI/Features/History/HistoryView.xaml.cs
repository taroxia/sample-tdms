using WpfUI.Core.Base;

namespace WpfUI.Features.History;

public partial class HistoryView : ViewBase<HistoryViewModel>
{
    public HistoryView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(HistoryViewModel? viewModel)
    {
        if (viewModel is null) return;
        this.DataContext = viewModel;
    }
}