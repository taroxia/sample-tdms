// ────────────────────────────────
//
// ────────────────────────────────

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Explorer;

public sealed class SkeletonExpViewModel : ExplorerViewModelBase
{
    private readonly SkeletonService _service;

    public BindableReactiveProperty<string?> SelectedChannel => _service.SelectedChannel;

    public ReactiveCommand<string> SelectChannelCommand { get; }

    public SkeletonExpViewModel(SkeletonService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        SelectChannelCommand = new ReactiveCommand<string>();
        SelectChannelCommand
            .Subscribe(ch => _service.SelectChannel(ch))
            .AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
    }
}
