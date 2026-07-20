// ────────────────────────────────
//
// ────────────────────────────────

using System;
using R3;
using WpfUI.Core.Base;
using WpfUI.Features.Skeleton.Documents.Charts;
using WpfUI.Features.Skeleton.Documents.Details;
using WpfUI.Features.Skeleton.Documents.Explorer;

namespace WpfUI.Features.Skeleton.Documents;

public sealed class SkeletonDocViewModel : DocumentViewModelBase
{
    private readonly SkeletonService _service;

    public SkeletonDocExpViewModel? ExplorerViewModel { get; }
    public SkeletonDocChartViewModel? ChartViewModel { get; }
    public SkeletonDocDetailViewModel? DetailViewModel { get; }

    public BindableReactiveProperty<double> ExplorerWidth => _service.ExplorerWidth;
    public BindableReactiveProperty<bool> IsExplorerExpanded => _service.IsExplorerExpanded;

    public ReactiveCommand<Unit> ToggleExplorerCommand { get; }

    public SkeletonDocViewModel(SkeletonService service)
        : base("Skeleton Workspace", "Skeleton_Doc_Root")
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        ExplorerViewModel = new SkeletonDocExpViewModel(service).AddTo(ref _disposables);
        ChartViewModel = new SkeletonDocChartViewModel(service).AddTo(ref _disposables);
        DetailViewModel = new SkeletonDocDetailViewModel(service).AddTo(ref _disposables);

        ToggleExplorerCommand = new ReactiveCommand<Unit>().AddTo(ref _disposables);
        ToggleExplorerCommand
            .Subscribe(_ =>
            {
                _service.IsExplorerExpanded.Value = !_service.IsExplorerExpanded.Value;
            })
            .AddTo(ref _disposables);

        ExplorerViewModel.SelectedNode
            .Where(node => node is not null)
            .Subscribe(node => ChartViewModel.LoadNodeStream(node!))
            .AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
    }
}
