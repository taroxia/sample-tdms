// ────────────────────────────────
//
// ────────────────────────────────

using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
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
        //        if (viewModel?.PlotInstance?.Value is not { } initialPlot) return;

        if (viewModel is null) return;

        viewModel.PlotInstance
            .Where(p => p is not null)
            .ObserveOn(new System.Windows.Threading.DispatcherSynchronizationContext(this.Dispatcher))
            .Subscribe(p =>
            {
                FormsPlot.Plot.Clear();
                FormsPlot.Reset(p!);
                FormsPlot.Refresh();
            })
            .AddTo(viewModel._disposables); // ViewModelの破棄に追従
    }
}
