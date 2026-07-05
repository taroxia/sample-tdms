// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using R3;
using ScottPlot;
using ScottPlot.WPF;

using WpfUI.Core.Abstractions;
using WpfUI.Core.Base;
using WpfUI.Core.Domain.Types;
using WpfUI.Infrastructure.Persistence.Tdms;

namespace WpfUI.Features.Waveform;

public partial class WaveformView : ViewBase<WaveformViewModel>
{
    private WaveformViewModel? _viewModel;
    private DisposableBag _disposables = new();
    private bool _isSyncingAxes = false;

    // ItemsControlへの参照保持
    private ItemsControl? _itemsControl;

    public WaveformView()
    {
        InitializeComponent();

        // 読み込み完了イベント
        Loaded += OnLoaded;
    }

    protected override void OnViewModelAttached(WaveformViewModel? vm)
    {
        if (DataContext is not WaveformViewModel viewModel) return;
        if (vm is null) return;

        _viewModel = vm;

        vm.Disposed
            .Subscribe(_ => OnDisposed())
            .AddTo(ref _disposables);
    }

    private void OnDisposed()
    {
        CleanUpPlots();

        if (_itemsControl != null && _itemsControl.ItemContainerGenerator != null)
        {
            _itemsControl.ItemContainerGenerator.StatusChanged -= OnGeneratorStatusChanged;
        }

        _disposables.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // ビジュアルツリーから一回だけItemsControlを確定させてイベントをフックする
        _itemsControl = FindVisualChild<ItemsControl>(this, string.Empty);
        if (_itemsControl != null)
        {
            // コンテナ生成ステータスの変更を監視（非同期ビジュアルツリー構築への追従）
            _itemsControl.ItemContainerGenerator.StatusChanged += OnGeneratorStatusChanged;
        }

        // 1. 再レンダリング要求ストリームの購読
        _viewModel.RequestRender
            .Subscribe(_ => Dispatcher.Invoke(QueueRenderAllPlots, System.Windows.Threading.DispatcherPriority.Background))
            .AddTo(ref _disposables);

        // 2. ViewModel の共有軸範囲の変更を全プロットに反映
        //_viewModel.SharedAxisLimits
        //    .Subscribe(limits => OnSharedXAxisLimitsChanged(limits))
        //    .AddTo(ref _disposables);
        _viewModel.SharedXLimits
            .Subscribe(xRange => OnSharedXRangeChanged(xRange))
            .AddTo(ref _disposables);

        // 初回の初期描画スケジュール
        QueueRenderAllPlots();
    }

    /// <summary>
    /// コンテナ生成ステータスが完了になった際に、安全に再描画とイベントバインドをトリガーする
    /// </summary>
    private void OnGeneratorStatusChanged(object? sender, EventArgs e)
    {
        if (_itemsControl == null) return;

        if (_itemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
        {
            // コンテナ生成直後、子要素（WpfPlotなど）の初期化を数ミリ秒待つため優先度を指定してスケジュール
            QueueRenderAllPlots();
        }
    }

    /// <summary>
    /// レースコンディションを回避するため、適切なDispatcherPriorityで描画処理をキューイングする
    /// </summary>
    private void QueueRenderAllPlots()
    {
        if (_viewModel == null) return;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            // ビジュアルツリーがまだ完全に構築されていないコンテナがあるか検証を兼ねて実行
            RenderAllPlots();
        }), System.Windows.Threading.DispatcherPriority.Render);
        // Renderプライオリティを使用することで、WPFのレイアウトパス・描画パスの直前に正確に同期させます
    }

    private void SetupDragDropHandlers()
    {
        foreach (var (layer, _, leftZone, rightZone, xZone) in GetActivePlotLayers())
        {
            if (leftZone != null) SetupZone(leftZone, layer, AxisPosition.Left);
            if (rightZone != null) SetupZone(rightZone, layer, AxisPosition.Right);
            if (xZone != null) SetupZone(xZone, layer, AxisPosition.X);
        }
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
                    await _viewModel.HandleChannelDropAsync(layer, pos);
                }
            }
        };
    }

    private async void RenderAllPlots()
    {
        if (_viewModel == null) return;

        var b = HasAxisByState();
        var c = HasAxis();

        // 全てのプロットのハンドラを一旦解除
        CleanUpPlots();

        var activeLayers = GetActivePlotLayers().ToList();
        if (activeLayers.Count() == 0) return; // ビジュアルツリーの準備が整っていない場合は抜ける（StatusChangedで再入されるため安全）

        // X軸の表示開始位置を一致させる
        int maxLeftAxes = 0;
        int maxRightAxes = 0;
        foreach (var (layer, _, _, _, _) in activeLayers)
        {
            int leftCount = layer.Assignments.Count(a => a.Position == AxisPosition.Left);
            int rightCount = layer.Assignments.Count(a => a.Position == AxisPosition.Right);

            if (leftCount > maxLeftAxes) maxLeftAxes = leftCount;
            if (rightCount > maxRightAxes) maxRightAxes = rightCount;
        }
        const float basePaddingLeft = 60f;
        const float basePaddingRight = 60f;

        // 最低限確保するマージン（軸が0でもドロップゾーンを確保するため）
        float finalPaddingLeft = Math.Max(70f, maxLeftAxes * basePaddingLeft);
        float finalPaddingRight = Math.Max(70f, maxRightAxes * basePaddingRight);

        foreach (var (layer, wpfPlot, _, _, _) in activeLayers)
        {
            if (layer == null || wpfPlot == null) continue;

            wpfPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#262F3D");
            wpfPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#374151");

            PixelPadding padding = new(finalPaddingLeft, finalPaddingRight, 20f, 40f); // 左, 右, 上, 下
            wpfPlot.Plot.Layout.LayoutEngine = new ScottPlot.LayoutEngines.FixedPadding(padding);

            var yAssignments = layer.Assignments.Where(a => a.Position != AxisPosition.X).ToList();
            if (yAssignments.Count() == 0)
            {
                // 全て削除された場合は、デフォルト軸の状態を完全に初期化する
                wpfPlot.Plot.Axes.Left.Label.Text = "No Data";
                wpfPlot.Plot.Axes.Right.Label.Text = string.Empty;

                // Bottom(X軸)もリセット
                wpfPlot.Plot.Axes.Bottom.Label.Text = "Time / Index";

                wpfPlot.Plot.Axes.AutoScale();
                wpfPlot.Refresh();
                continue; // 次のレイヤー処理へ
            }

            // X軸データの抽出
            var xAssignment = layer.Assignments.FirstOrDefault(a => a.Position == AxisPosition.X);
            var isTimestamp = xAssignment?.Channel.DataType == DataType.Timestamp;
            double[] xData = Array.Empty<double>();

            if (xAssignment != null)
            {
                xData = await _viewModel.LoadChannelDataAsync(xAssignment.Channel);
            }

            // Y軸データの描画
            int leftAxisCount = 0;
            int rightAxisCount = 0;
            foreach (var assign in layer.Assignments.Where(a => a.Position != AxisPosition.X))
            {
                double[] yData = await _viewModel.LoadChannelDataAsync(assign.Channel);
                if (yData.Length == 0) continue;

                double[] finalX = (xData.Length == yData.Length) ? xData : GenerateIndexAxis(yData.Length);

                // ScottPlot 5 厳格仕様に従ったプロット構成
                var signal = wpfPlot.Plot.Add.SignalXY(finalX, yData);
                signal.MarkerStyle = MarkerStyle.None;

                if (assign.Position == AxisPosition.Left)
                {
                    if (leftAxisCount == 0)
                    {
                        // 最初は標準の左軸を設定
                        wpfPlot.Plot.Axes.Left.Label.Text = assign.Channel.Name;
                        wpfPlot.Plot.Axes.Left.Label.ForeColor = ScottPlot.Color.FromHex("#0ea5e9"); // Cyber Cyan
                        signal.Axes.YAxis = wpfPlot.Plot.Axes.Left;
                    }
                    else
                    {
                        var customAxis = wpfPlot.Plot.Axes.AddLeftAxis();
                        customAxis.LabelText = assign.Channel.Name;
                        customAxis.LabelFontColor = ScottPlot.Color.FromHex("#0ea5e9"); // Cyber Cyan
                        signal.Axes.YAxis = customAxis;
                    }
                    leftAxisCount++;
                }
                else if (assign.Position == AxisPosition.Right)
                {
                    if (rightAxisCount == 0)
                    {
                        // 最初は標準の右軸を設定
                        wpfPlot.Plot.Axes.Right.Label.Text = assign.Channel.Name;
                        wpfPlot.Plot.Axes.Right.Label.ForeColor = ScottPlot.Color.FromHex("#0ea5e9");
                        signal.Axes.YAxis = wpfPlot.Plot.Axes.Right;
                    }
                    else
                    {
                        var customAxis = wpfPlot.Plot.Axes.AddRightAxis();
                        customAxis.LabelText = assign.Channel.Name;
                        customAxis.LabelFontColor = ScottPlot.Color.FromHex("#0ea5e9");
                        signal.Axes.YAxis = customAxis;
                    }
                    rightAxisCount++;
                }
            }
            if (isTimestamp)
            {
                wpfPlot.Plot.Axes.DateTimeTicksBottom();
            }

            var d = HasAxisByState();
            var e = HasAxis();

            var sharedX = _viewModel.SharedXLimits.Value;
            if (!double.IsNaN(sharedX.Min) && !double.IsNaN(sharedX.Max))
            {
                wpfPlot.Plot.Axes.SetLimitsX(sharedX.Min, sharedX.Max);
            }
            else
            {
                wpfPlot.Plot.Axes.AutoScaleX();
            }


            //// ★改良：永続化されている個別LayerのLimits（X/Y軸双方含む）が存在すれば最優先で復元
            //if (layer.CurrentLimits.HasArea)
            //{
            //    wpfPlot.Plot.Axes.SetLimits(layer.CurrentLimits);
            //}
            //else
            //{
            //    wpfPlot.Plot.Axes.AutoScale();
            //    // 初回オートスケール時の限界値を保存しておく
            //    layer.CurrentLimits = wpfPlot.Plot.Axes.GetLimits();
            //}

            //var currentLimits = _viewModel.SharedAxisLimits.Value;
            //if (currentLimits.HasArea)
            //{
            //    wpfPlot.Plot.Axes.SetLimits(currentLimits);
            //    wpfPlot.Plot.Axes.SetLimitsY(currentLimits);
            //}
            //else
            //{
            //    wpfPlot.Plot.Axes.AutoScale();
            //}

            if (double.IsNaN(sharedX.Min) || double.IsNaN(sharedX.Max))
            {
                var currentXLimits = wpfPlot.Plot.Axes.GetLimits();
                _viewModel.SharedXLimits.Value = new CoordinateRange(currentXLimits.Left, currentXLimits.Right);
            }

            wpfPlot.Refresh();

            wpfPlot.UserInputProcessor.IsEnabled = true;
            wpfPlot.Tag = layer;
            wpfPlot.Plot.RenderManager.RenderFinished += OnPlotRenderFinished;
        }

        // ドロップゾーンのハンドラ再割り当て
        SetupDragDropHandlers();
    }


    private void OnPlotRenderFinished(object? sender, RenderDetails details)
    {
        if (_isSyncingAxes || _viewModel == null || sender is not Plot plot) return;

        var activeLayers = GetActivePlotLayers().ToList();
        var currentTarget = activeLayers.FirstOrDefault(x => x.WpfPlot.Plot == plot);
        //if (currentTarget.Layer == null || currentTarget.WpfPlot.Tag is not (PlotLayerModel _, List<(PlotAssignment Assignment, ScottPlot.Plottables.SignalXY Signal, ScottPlot.AxisPanels.IYAxis Axis)> mappings)) return;

        _isSyncingAxes = true;
        try
        {
            // 1. ユーザー操作（パン・ズーム）によって変化した最新の「Y軸個別スケール」を、割当モデルへ即座に完全同期
            //foreach (var mapping in mappings)
            //{
            //    var currentRange = new CoordinateRange(mapping.Axis.Range.Min, mapping.Axis.Range.Max);
            //    if (!IsApproxEqual(mapping.Assignment.YRange.Value.Min, currentRange.Min) ||
            //        !IsApproxEqual(mapping.Assignment.YRange.Value.Max, currentRange.Max))
            //    {
            //        mapping.Assignment.YRange.Value = currentRange;
            //    }
            //}

            // 2. ユーザー操作によって「X軸」が変化した場合、グローバルな共有プロパティへ伝播（全段一斉同期のトリガー）
            AxisLimits currentLimits = plot.Axes.GetLimits();
            var globalX = _viewModel.SharedXLimits.Value;

            if (!IsApproxEqual(globalX.Min, currentLimits.Left) || !IsApproxEqual(globalX.Max, currentLimits.Right))
            {
                _viewModel.SharedXLimits.Value = new CoordinateRange(currentLimits.Left, currentLimits.Right);
            }
        }
        finally
        {
            _isSyncingAxes = false;
        }
    }
#if false
    private void OnPlotRenderFinished(object? sender, RenderDetails details)
    {
        if (_isSyncingAxes || _viewModel == null) return;
        if (sender is not Plot plot) return;

        // 対象プロットが所属するレイヤーモデルをTagから安全に取得
        var activeLayers = GetActivePlotLayers().ToList();
        var currentTarget = activeLayers.FirstOrDefault(x => x.WpfPlot.Plot == plot);
        if (currentTarget.Layer == null) return;

        AxisLimits currentLimits = plot.Axes.GetLimits();
        var previousLimits = currentTarget.Layer.CurrentLimits;

        if (IsApproxEqual(previousLimits, currentLimits)) return;

        // ★改良: 操作された段の最新Limits（Y軸のスケール含む）を対応するLayerへ即座に完全保存
        currentTarget.Layer.CurrentLimits = currentLimits;

        _isSyncingAxes = true;
        try
        {
            // X軸が変更された場合のみ、他のプロットへX軸の同期伝搬を行う
            var isXRangeChanged = !IsApproxEqual(previousLimits.Left, currentLimits.Left) ||
                                  !IsApproxEqual(previousLimits.Right, currentLimits.Right);
            if (isXRangeChanged)
            {
                PropagateXLimitsToOtherPlots(plot, currentLimits);

                // ViewModelの持つ共有X軸情報も更新（他の画面への配慮や初期値共有用）
                _viewModel.SharedAxisLimits.Value = currentLimits;
            }
        }
        finally
        {
            _isSyncingAxes = false;
        }
    }
#endif
    private void PropagateXLimitsToOtherPlots(Plot sourcePlot, AxisLimits limits)
    {
        foreach (var (layer, wpfPlot, _, _, _) in GetActivePlotLayers())
        {
            if (wpfPlot == null || wpfPlot.Plot == sourcePlot) continue;

            var current = wpfPlot.Plot.Axes.GetLimits();
            if (IsApproxEqual(limits.Left, current.Left) && IsApproxEqual(limits.Right, current.Right)) continue;

            // X軸のみを同期し、各段固有のY軸範囲（Top, Bottom）は完全に維持する
            wpfPlot.Plot.Axes.SetLimitsX(limits.Left, limits.Right);

            // ★重要: 同期された新しい状態（X軸が変わり、Y軸はそのまま）をLayer側のLimitsへもマージして保存
            layer.CurrentLimits = wpfPlot.Plot.Axes.GetLimits();

            wpfPlot.Refresh();
        }
    }
#if false
    private void OnSharedXAxisLimitsChanged(AxisLimits limits)
    {
        if (!limits.HasArea || _isSyncingAxes || !HasAxisByState()) return;
        _isSyncingAxes = true;

        try
        {
            foreach (var (layer, wpfPlot, _, _, _) in GetActivePlotLayers())
            {
                if (wpfPlot == null) continue;

                var current = wpfPlot.Plot.Axes.GetLimits();
                if (IsApproxEqual(limits.Left, current.Left) && IsApproxEqual(limits.Right, current.Right)) continue;

                wpfPlot.Plot.Axes.SetLimitsX(limits.Left, limits.Right);
                layer.CurrentLimits = wpfPlot.Plot.Axes.GetLimits();
                wpfPlot.Refresh();
            }
        }
        finally
        {
            _isSyncingAxes = false;
        }
    }
#endif
    private void OnSharedXRangeChanged(CoordinateRange xRange)
    {
        if (double.IsNaN(xRange.Min) || double.IsNaN(xRange.Max) || _isSyncingAxes) return;
        _isSyncingAxes = true;

        try
        {
            foreach (var (_, wpfPlot, _, _, _) in GetActivePlotLayers())
            {
                if (wpfPlot == null) continue;

                var currentX = wpfPlot.Plot.Axes.GetLimits();
                if (IsApproxEqual(xRange.Min, currentX.Left) && IsApproxEqual(xRange.Max, currentX.Right)) continue;

                // 各段固有のY軸範囲（Top, Bottom）は完全に維持したまま、X軸の限界値のみを一斉置換
                wpfPlot.Plot.Axes.SetLimitsX(xRange.Min, xRange.Max);
                wpfPlot.Refresh();
            }
        }
        finally
        {
            _isSyncingAxes = false;
        }
    }

    private void CleanUpPlots()
    {
        if (_itemsControl == null) return;

        for (int i = 0; i < _itemsControl.Items.Count; i++)
        {
            if (_itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                continue;
            if (FindVisualChild<WpfPlot>(container, "PlotControl") is not WpfPlot wpfPlot)
                continue;

            wpfPlot.Plot.RenderManager.RenderFinished -= OnPlotRenderFinished!;
            wpfPlot.Plot.Clear();

            var customAxes = wpfPlot.Plot.Axes.GetAxes()
                .Where(ax => ax != wpfPlot.Plot.Axes.Bottom
                          && ax != wpfPlot.Plot.Axes.Left
                          && ax != wpfPlot.Plot.Axes.Top
                          && ax != wpfPlot.Plot.Axes.Right)
                .ToList();
            foreach (var axis in customAxes)
            {
                wpfPlot.Plot.Axes.Remove(axis);
            }
        }
    }

    private bool HasAxis()
    {
        if (_itemsControl == null) return false;

        var wpfPlots = Enumerable.Range(0, _itemsControl.Items.Count)
            .Select(_itemsControl.ItemContainerGenerator.ContainerFromIndex)
            .OfType<FrameworkElement>()
            .Select(container => FindVisualChild<WpfPlot>(container, "PlotControl"))
            .OfType<WpfPlot>();

        foreach (var wpfPlot in wpfPlots)
        {
            if (wpfPlot.Plot.Axes.GetAxes()
                .Where(ax => ax != wpfPlot.Plot.Axes.Bottom
                          && ax != wpfPlot.Plot.Axes.Left
                          && ax != wpfPlot.Plot.Axes.Top
                          && ax != wpfPlot.Plot.Axes.Right)
                .ToList().Count > 0) return true;
        }
        return false;
    }

    private bool HasAxisByState()
    {
        if (_viewModel == null) return false;
        return _viewModel.PlotLayers.Any(layer =>
            layer.Assignments.Any(assign => assign.Position == AxisPosition.Left || assign.Position == AxisPosition.Right));
    }

    private IEnumerable<(PlotLayerModel Layer, WpfPlot WpfPlot, Border? LeftZone, Border? RightZone, Border? XZone)> GetActivePlotLayers()
    {
        if (_itemsControl == null) yield break;

        int count = _itemsControl.Items.Count;
        for (int i = 0; i < count; i++)
        {
            if (_itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                continue;

            if (container.DataContext is not PlotLayerModel layer)
                continue;

            //var wpfPlot = FindVisualChild<WpfPlot>(container, "PlotControl");
            //if (wpfPlot == null) continue;

            if (FindVisualChild<WpfPlot>(container, "PlotControl") is not WpfPlot wpfPlot)
                continue;

            var leftZone = FindVisualChild<Border>(container, "LeftDropZone");
            var rightZone = FindVisualChild<Border>(container, "RightDropZone");
            var xZone = FindVisualChild<Border>(container, "XDropZone");

            yield return (layer, wpfPlot, leftZone, rightZone, xZone);
        }
    }

    private double[] GenerateIndexAxis(int length)
    {
        double[] axis = new double[length];
        for (int i = 0; i < length; i++) axis[i] = i;
        return axis;
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

    static bool IsApproxEqual(double a, double b) => Math.Abs(a - b) <= 1e-6;

    static bool IsApproxEqual(AxisLimits a, AxisLimits b) =>
        IsApproxEqual(a.Left, b.Left) &&
        IsApproxEqual(a.Right, b.Right) &&
        IsApproxEqual(a.Top, b.Top) &&
        IsApproxEqual(a.Bottom, b.Bottom);
}
