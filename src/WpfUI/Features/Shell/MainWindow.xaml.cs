// ────────────────────────────────
//
// ────────────────────────────────

using System.Windows;
using System.Windows.Media;

namespace WpfUI.Features.Shell;

// ViewModel は DI から注入される
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;

        viewModel.Navigation.IsSidebarExpanded.Subscribe(isExpanded =>
        {
            //    VisualStateManager.GoToElementState(this, isExpanded ? "Expanded" : "Collapsed", true);
            VisualStateManager.GoToElementState(Sidebar, isExpanded ? "Expanded" : "Collapsed", true);
            ToggleIcon.Data = (Geometry)FindResource(isExpanded ? "Icon.ChevronLeft" : "Icon.ChevronRight");
        });
    }
    public MainWindow()
    {
        InitializeComponent();
    }
}
