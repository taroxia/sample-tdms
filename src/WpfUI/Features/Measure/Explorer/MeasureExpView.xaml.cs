using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfUI.Core.Base;

namespace WpfUI.Features.Measure.Explorer;

public partial class MeasureExpView : ViewBase<MeasureExpViewModel>
{
    public MeasureExpView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(MeasureExpViewModel? viewModel)
    {
        if (viewModel is null) return;
        this.DataContext = viewModel;
    }

    private void Node_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is ListBoxItem item && item.DataContext != null)
        {
            DragDrop.DoDragDrop(item, item.DataContext.ToString(), DragDropEffects.Copy);
        }
    }
}