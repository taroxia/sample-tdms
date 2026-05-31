// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using R3;
using ScottPlot;
using ScottPlot.WPF;

using WpfUI.Core.Abstractions;
using WpfUI.Core.Base;
using WpfUI.Core.Dmain.Models;
using WpfUI.Infrastructure.Persistence.Tdms;

namespace WpfUI.Features.Waveform;

public partial class WaveformView : ViewBase<WaveformViewModel>
{
    private WaveformViewModel? _viewModel;
    private DisposableBag _disposables = new();
    private bool _isUpdatingLimits = false;
    private bool _isUpdatingZone = false;

    public WaveformView()
    {
        InitializeComponent();

        // ItemsControlのジェネレートタイミングに合わせたイベントハンドリング
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    protected override void OnViewModelAttached(WaveformViewModel? vm)
    {
        if (DataContext is not WaveformViewModel viewModel) return;
        if (vm is null) return;

        _viewModel = vm;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // 再レンダリング要求ストリームの購読
        _viewModel.RequestRender
            .Subscribe(_ => Dispatcher.Invoke(RenderAllPlots))
            .AddTo(ref _disposables);

        // 各段の初期セットアップ（UIツリーを走査）
        Dispatcher.BeginInvoke(new Action(() =>
        {
            SetupDragDropHandlers();
            RenderAllPlots();
            SyncAxisLimits();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _disposables.Dispose();
    }

    private void SetupDragDropHandlers()
    {
        // ItemsControl 内の全要素を走査してドロップイベントを登録
        var itemsControl = LogicalTreeHelper.GetChildren(this).OfType<Grid>().FirstOrDefault()
            ?.Children.OfType<ItemsControl>().FirstOrDefault();

        if (itemsControl == null || _isUpdatingZone) return;
        _isUpdatingZone = true;

        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            if (container == null) continue;

            var model = container.DataContext as PlotLayerModel;
            if (model == null) continue;

            var leftZone = FindVisualChild<Border>(container, "LeftDropZone");
            var rightZone = FindVisualChild<Border>(container, "RightDropZone");
            var xZone = FindVisualChild<Border>(container, "XDropZone");

            if (leftZone != null) SetupZone(leftZone, model, AxisPosition.Left);
            if (rightZone != null) SetupZone(rightZone, model, AxisPosition.Right);
            if (xZone != null) SetupZone(xZone, model, AxisPosition.X);
        }
        _isUpdatingZone = false;
    }

    private void SetupZone(Border zone, PlotLayerModel layer, AxisPosition pos)
    {

        zone.DragOver += (s, e) =>
        {
            e.Effects = e.Data.GetDataPresent("ArcticSlate.TdmsChannel") ? DragDropEffects.Copy : DragDropEffects.None;
            zone.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 173, 181)); // Highlight Cyan
            e.Handled = true;
        };

        zone.DragLeave += (s, e) =>
        {
            zone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x15, 0x1C, 0x25));
        };

        zone.Drop += async (s, e) =>
        {
            zone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x15, 0x1C, 0x25));
            if (e.Data.GetData("ArcticSlate.TdmsChannel") is TdmsChannelMetadata channel)
            {
                if (_viewModel != null)
                {
                    await _viewModel.HandleChannelDropAsync(layer, channel, pos);
                    SetupDragDropHandlers(); // 再構築
                }
            }
        };

    }

    private async void RenderAllPlots()
    {
        if (_viewModel == null) return;

        var itemsControl = FindVisualChild<ItemsControl>(this, "");
        if (itemsControl == null) return;

        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            if (container == null) continue;

            var layer = container.DataContext as PlotLayerModel;
            var wpfPlot = FindVisualChild<WpfPlot>(container, "PlotControl");
            if (layer == null || wpfPlot == null) continue;

            // ScottPlot 5 公式推奨初期化パターン
#if false
            Plot plot = new();
            plot.FigureBackground.Color = ScottPlot.Color.FromHex("#262F3D");
            plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#374151");
#else
            wpfPlot.Plot.Clear();
            wpfPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#262F3D");
            wpfPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#374151");

#endif

            // X軸データの抽出
            var xAssignment = layer.Assignments.FirstOrDefault(a => a.Position == AxisPosition.X);
            var isTimestamp = xAssignment?.Channel.DataType == DataType.Timestamp;
            double[] xData = Array.Empty<double>();
            if (xAssignment != null)
            {
                xData = await _viewModel.LoadChannelDataAsync(xAssignment.Channel);
            }

            // Y軸データの描画
            foreach (var assign in layer.Assignments.Where(a => a.Position != AxisPosition.X))
            {
                double[] yData = await _viewModel.LoadChannelDataAsync(assign.Channel);
                if (yData.Length == 0) continue;

                // Xデータが未アサインまたはサイズ不一致の場合はインデックス生成
                double[] finalX = (xData.Length == yData.Length) ? xData : GenerateIndexAxis(yData.Length);

                // ScottPlot 5 厳格API呼出
                var signal = wpfPlot.Plot.Add.SignalXY(finalX, yData);
                signal.MarkerStyle = MarkerStyle.None;

                if (assign.Position == AxisPosition.Left)
                {
                    var customAxis = wpfPlot.Plot.Axes.AddLeftAxis();
                    customAxis.LabelText = assign.Channel.Name;
                    customAxis.LabelFontColor = ScottPlot.Colors.Cyan;
                    signal.Axes.YAxis = customAxis;
                }
                else if (assign.Position == AxisPosition.Right)
                {
                    var customAxis = wpfPlot.Plot.Axes.AddRightAxis();
                    customAxis.LabelText = assign.Channel.Name;
                    customAxis.LabelFontColor = ScottPlot.Colors.Magenta;
                    signal.Axes.YAxis = customAxis;
                }
            }
            if (isTimestamp)
            {
                wpfPlot.Plot.Axes.DateTimeTicksBottom();
            }
            else
            {
                //wpfPlot.Plot.Axes.LinearTicksBottom();
            }

            wpfPlot.Refresh();
        }

        SyncAxisLimits();
    }

    private double[] GenerateIndexAxis(int length)
    {
        double[] axis = new double[length];
        for (int i = 0; i < length; i++) axis[i] = i;
        return axis;
    }

    private void SyncAxisLimits()
    {
        if (_viewModel == null) return;

        var itemsControl = FindVisualChild<ItemsControl>(this, "");
        if (itemsControl == null) return;

        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            var wpfPlot = FindVisualChild<WpfPlot>(container, "PlotControl");
            if (wpfPlot == null) continue;

            // 各Plotの軸操作のリアルタイム検知 (ScottPlot 5 仕様)
            wpfPlot.UserInputProcessor.IsEnabled = true;

            // マウス操作などで制限が変わった場合の同調イベント
            wpfPlot.SizeChanged += (s, e) => PropagateLimits(wpfPlot);
        }
    }

    private void PropagateLimits(WpfPlot sourcePlot)
    {
        if (_isUpdatingLimits || _viewModel == null) return;
        _isUpdatingLimits = true;

        var currentLimits = sourcePlot.Plot.Axes.GetLimits();
        _viewModel.SharedAxisLimits.Value = currentLimits;

        var itemsControl = FindVisualChild<ItemsControl>(this, "");
        if (itemsControl != null)
        {
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                var wpfPlot = FindVisualChild<WpfPlot>(container, "PlotControl");
                if (wpfPlot != null && wpfPlot != sourcePlot)
                {
                    wpfPlot.Plot.Axes.SetLimitsX(currentLimits.Left, currentLimits.Right);
                    wpfPlot.Refresh();
                }
            }
        }
        _isUpdatingLimits = false;
    }

    // WPFビジュアルツリー検索ユーティリティ
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
