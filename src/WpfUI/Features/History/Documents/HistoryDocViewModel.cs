using System;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.History.Documents;

public sealed class HistoryDocViewModel : DocumentViewModelBase
{
    private readonly HistoryService _service;

    // R3の適切なBindableReactivePropertyの公開
    public BindableReactiveProperty<string?> TargetContent => _service.SharedTargetContent;
    public ReactiveCommand<string> SetTargetCommand { get; }

    public HistoryDocViewModel(HistoryService service) : base("HistoryDoc", "History Document")
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        SetTargetCommand = new ReactiveCommand<string>();
        SetTargetCommand
            .Subscribe(content => _service.UpdateTargetContent(content))
            .AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
    }
}