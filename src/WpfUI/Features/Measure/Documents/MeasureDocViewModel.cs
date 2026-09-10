// ────────────────────────────────
//
// ────────────────────────────────

using System;
using R3;
using WpfUI.Core.Abstractions;
using WpfUI.Core.Base;
using WpfUI.Features.Measure.Waveform;
using WpfUI.Features.Measure.Waveform.Charts;
using WpfUI.Features.Measure.Waveform.Details;
using WpfUI.Features.Measure.Waveform.Explorer;

namespace WpfUI.Features.Measure.Documents;

public sealed class MeasureDocViewModel : DocumentViewModelBase
{
    private readonly WaveformService _service;

    public WaveformExpViewModel? ExplorerViewModel { get; }
    public WaveformChartViewModel? ChartViewModel { get; }
    public WaveformDetailViewModel? DetailViewModel { get; }

    public BindableReactiveProperty<double> ExplorerWidth => _service.ExplorerWidth;
    public BindableReactiveProperty<bool> IsExplorerExpanded => _service.IsExplorerExpanded;

    public ReactiveCommand<Unit> ToggleExplorerCommand { get; }

    public MeasureDocViewModel(WaveformService service, ITdmsService tdms)
        : base("Measure Workspace", "Measure_Doc_Root")
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        ExplorerViewModel = new WaveformExpViewModel(service, tdms).AddTo(ref _disposables);
        ChartViewModel = new WaveformChartViewModel(service).AddTo(ref _disposables);
        DetailViewModel = new WaveformDetailViewModel(service).AddTo(ref _disposables);

        ToggleExplorerCommand = new ReactiveCommand<Unit>().AddTo(ref _disposables);
        ToggleExplorerCommand
            .Subscribe(_ =>
            {
                _service.IsExplorerExpanded.Value = !_service.IsExplorerExpanded.Value;
            })
            .AddTo(ref _disposables);

        //ExplorerViewModel.SelectedNode
        //    .Where(node => node is not null)
        //    .Subscribe(node => ChartViewModel.LoadNodeStream(node!))
        //    .AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
    }
}
