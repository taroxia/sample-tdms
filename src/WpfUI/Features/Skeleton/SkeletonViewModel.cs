using System;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton;

public sealed class SkeletonViewModel : FeatureViewModelBase
{
    private readonly SkeletonService _service;

    // 右下ペイン専用のステート同期構造
    public BindableReactiveProperty<string?> ObservedTarget => _service.SharedTargetContent;
    public ReactiveCommand<Unit> ClearStateCommand { get; }

    public SkeletonViewModel(SkeletonService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        ClearStateCommand = new ReactiveCommand<Unit>();
        ClearStateCommand
            .Subscribe(_ =>
            {
                _service.SelectedNode.Value = null;
                _service.UpdateTargetContent(null);
            })
            .AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
    }
}