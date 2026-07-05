// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using R3;
using ScottPlot;
using ScottPlot.WPF;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Documents;

public partial class SkeletonDocView : ViewBase<SkeletonDocViewModel>
{
    private ItemsControl? _itemsControl;

    public SkeletonDocView()
    {
        InitializeComponent();
    }

    protected override void OnViewModelAttached(SkeletonDocViewModel? viewModel)
    {
        if (viewModel is null) return;
        this.DataContext = viewModel;
        // 画面起動直後の初期プロットセットアップ (ScottPlot 5 公式推奨パターン)
        Plot plot = new();

        // サンプルシグナル生成のためのモックデータ
        double[] dataX = { 1, 2, 3, 4, 5 };
        double[] dataY = { 2, 4, 1, 5, 3 };

        // ScottPlot 5 実在APIの厳密な使用
        plot.Add.Scatter(dataX, dataY);
        //MainWpfPlot.Plot = plot;
        _itemsControl = FindVisualChild<ItemsControl>(this, string.Empty);
        if (_itemsControl == null) return;
        if (_itemsControl.ItemContainerGenerator.ContainerFromIndex(0) is not FrameworkElement container)
            return;
        if (FindVisualChild<WpfPlot>(container, "PlotControl") is not WpfPlot wpfPlot)
            return;

        wpfPlot.Refresh();

        // ビューロード時にViewModelとのReactive同期を取る
        this.Loaded += (s, e) => InitializeBinding();
    }

    private void InitializeBinding()
    {
        if (DataContext is SkeletonDocViewModel vm)
        {
            // R3を用いた高速ステート復元
            vm.XAxisChannel.Subscribe(ch =>
            {
                if (!string.IsNullOrEmpty(ch))
                {
                    //XAxisBadge.Visibility = Visibility.Visible;
                    //TxtXAxis.Text = $"X: {ch}";

                    //// ScottPlot 5 の軸ラベル設定更新
                    //MainWpfPlot.Plot.Axes.Bottom.Label.Text = ch;
                    //MainWpfPlot.Refresh();
                }
                else
                {
                    //XAxisBadge.Visibility = Visibility.Collapsed;
                    //MainWpfPlot.Plot.Axes.Bottom.Label.Text = string.Empty;
                    //MainWpfPlot.Refresh();
                }
            });
        }
    }

    private void XAxisZone_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.StringFormat) && DataContext is SkeletonDocViewModel vm)
        {
            string channel = (string)e.Data.GetData(DataFormats.StringFormat);
            vm.DropXAxisCommand.Execute(channel);
        }
    }

    private void NewLayerZone_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is SkeletonDocViewModel vm)
        {
            vm.AddLayerCommand.Execute(Unit.Default);
            // 実際の実装ではここでScottPlot内のレイアウト追加ロジックを回す
        }
    }

    private void RemoveXAxis_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SkeletonDocViewModel vm)
        {
            vm.RemoveXAxisCommand.Execute(Unit.Default);
        }
    }

    private T? FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t && (string.IsNullOrEmpty(name) || (child is FrameworkElement fe && fe.Name == name)))
                return t;

            var childOfChild = FindVisualChild<T>(child, name);
            if (childOfChild != null) return childOfChild;
        }
        return null;
    }
}
