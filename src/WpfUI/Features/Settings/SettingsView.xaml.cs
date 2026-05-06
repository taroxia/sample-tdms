using System.Windows.Controls;

namespace WpfUI.Features.Settings;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
    public SettingsView()
    {
        InitializeComponent();
    }
}