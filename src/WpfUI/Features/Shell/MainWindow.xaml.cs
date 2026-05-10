// ────────────────────────────────
//
// ────────────────────────────────

using System.Windows;

namespace WpfUI.Features.Shell;

// ViewModel は DI から注入される
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
    public MainWindow()
    {
        InitializeComponent();
    }
}
