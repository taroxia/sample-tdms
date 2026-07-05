// ────────────────────────────────
//
// ────────────────────────────────

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using AvalonDock.Layout;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Shell;

// ViewModel は DI から注入される
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;

        viewModel.Navigation.IsSidebarExpanded
            .Subscribe(isExpanded =>
            {
                VisualStateManager.GoToElementState(Sidebar, isExpanded ? "Expanded" : "Collapsed", true);
            });
    }
    public MainWindow()
    {
        InitializeComponent();
    }
}
