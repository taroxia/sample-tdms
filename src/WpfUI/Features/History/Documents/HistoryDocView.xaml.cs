using System.Windows;
using WpfUI.Core.Base;

namespace WpfUI.Features.History.Documents;

public partial class HistoryDocView : ViewBase<HistoryDocViewModel>
{
    public HistoryDocView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(HistoryDocViewModel? viewModel)
    {
        if (viewModel is null) return;
        this.DataContext = viewModel;
    }

    private void WorkspaceZone_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.StringFormat) && DataContext is HistoryDocViewModel vm)
        {
            var content = (string)e.Data.GetData(DataFormats.StringFormat);
            vm.SetTargetCommand.Execute(content);
        }
    }
}