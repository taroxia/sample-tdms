using System;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.History;

public sealed class HistoryViewModel : FeatureViewModelBase
{
    private readonly HistoryService _service;

    // 右下ペイン専用のステート同期構造
    public BindableReactiveProperty<string?> ObservedTarget => _service.SharedTargetContent;
    public ReactiveCommand<Unit> ClearStateCommand { get; }

    public HistoryViewModel(HistoryService service)
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