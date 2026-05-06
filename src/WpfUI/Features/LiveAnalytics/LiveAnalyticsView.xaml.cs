using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using WpfUI.Core.Base;

namespace WpfUI.Features.LiveAnalytics;

public partial class LiveAnalyticsView : ViewBase<LiveAnalyticsViewModel>
{
    public LiveAnalyticsView() : base()
    {
        InitializeComponent();

    }

    protected override void OnViewModelAttached(LiveAnalyticsViewModel? viewModel)
    {
        if (viewModel == null) return;

        //DataContext = viewModel;
        viewModel.PlotInstance
            .Where(p => p != null)
            .ObserveOnUIDispatcher()
            .Subscribe(p =>
            {
                // ScottPlot 5 では、既存のPlotオブジェクトをリセットして再描画
                FormsPlot.Plot.Clear();
                // 内部のプロットロジックを同期（簡易実装例）
                FormsPlot.Reset(p!);
                FormsPlot.Refresh();
            })
            .AddTo(viewModel._disposables); // ViewModelの破棄に追従
    }
}
