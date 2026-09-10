// ────────────────────────────────
//
// ────────────────────────────────

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfUI.Core.Base;
using WpfUI.Features.History.Models;

namespace WpfUI.Features.History.Explorer;

public partial class HistoryExpView : ViewBase<HistoryExpViewModel>
{
    private Point _startPoint;

    public HistoryExpView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(HistoryExpViewModel? viewModel)
    {
        if (viewModel is null) return;
        this.DataContext = viewModel;
    }

    /// <summary>
    /// ListBox のマルチセレクト状態を ViewModel の SelectedItems / SelectedCount へ同期
    /// </summary>
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return;

        ViewModel.SelectedItems.Clear();
        foreach (HistoryItemRecord item in HistoryListBox.SelectedItems)
        {
            ViewModel.SelectedItems.Add(item);
        }
        ViewModel.SelectedCount.Value = ViewModel.SelectedItems.Count;
    }

    /// <summary>
    /// D&D 開始位置の記録
    /// </summary>
    private void OnCardPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
    }

    /// <summary>
    /// 右下ドッキング領域へのドラッグ＆ドロップ操作のハンドリング
    /// </summary>
    private void OnCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        Point mousePos = e.GetPosition(null);
        Vector diff = _startPoint - mousePos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (HistoryListBox.SelectedItem is HistoryItemRecord selectedRecord)
            {
                // ドラッグペイロードの作成
                var dragData = new DataObject("HistoryItemRecord", selectedRecord);
                DragDrop.DoDragDrop(HistoryListBox, dragData, DragDropEffects.Copy);
            }
        }
    }
}
