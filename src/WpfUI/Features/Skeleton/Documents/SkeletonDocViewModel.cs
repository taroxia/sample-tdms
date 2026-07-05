// ────────────────────────────────
//
// ────────────────────────────────

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using R3;
using WpfUI.Core.Base;
using WpfUI.Features.Skeleton;

namespace WpfUI.Features.Skeleton.Documents;

public sealed class SkeletonDocViewModel : DocumentViewModelBase
{
    private readonly SkeletonService _service;

    public ReadOnlyReactiveProperty<string?> XAxisChannel => _service.XAxisChannel;
    public ReadOnlyReactiveProperty<int> PlotLayersCount => _service.PlotLayersCount;

    public ReactiveCommand<string> DropXAxisCommand { get; }
    public ReactiveCommand<Unit> AddLayerCommand { get; }
    public ReactiveCommand<Unit> RemoveXAxisCommand { get; }

    public SkeletonDocViewModel(SkeletonService service) : base("Skeleton", "Skeleton")
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        DropXAxisCommand = new ReactiveCommand<string>();
        DropXAxisCommand.Subscribe(ch => _service.AssignXAxis(ch)).AddTo(ref _disposables);

        AddLayerCommand = new ReactiveCommand<Unit>();
        AddLayerCommand.Subscribe(_ => _service.AddPlotLayer()).AddTo(ref _disposables);

        RemoveXAxisCommand = new ReactiveCommand<Unit>();
        RemoveXAxisCommand.Subscribe(_ => _service.AssignXAxis(null)).AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
    }
}
