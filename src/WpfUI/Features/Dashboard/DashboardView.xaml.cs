using System.Windows.Controls;

namespace WpfUI.Features.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel viewModel) : this()
    {
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
    public DashboardView()
    {
        InitializeComponent();
    }
}
