// ────────────────────────────────
//
// ────────────────────────────────

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
// ---.
using WpfUI.Core.Base;
using WpfUI.Features.Skeleton;
using WpfUI.Features.Skeleton.Documents;

namespace WpfUI.Features.Skeleton.Explorer;

public partial class SkeletonExpView : ViewBase<SkeletonExpViewModel>
{
    public SkeletonExpView()
    {
        InitializeComponent();
    }

    private void Channel_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is ListBoxItem item)
        {
            DragDrop.DoDragDrop(item, item.Content.ToString(), DragDropEffects.Copy);
        }
    }
}
