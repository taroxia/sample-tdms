using System;
using System.Collections.Generic;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Skeleton.Explorer;

public sealed class SkeletonExpViewModel : ExplorerViewModelBase
{
    private readonly SkeletonService _service;

    public BindableReactiveProperty<string?> SelectedNode => _service.SelectedNode;
    public List<string> AvailableNodes { get; } = ["Node_Alpha_01", "Node_Beta_02", "Node_Gamma_03"];

    public SkeletonExpViewModel(SkeletonService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        // 選択変更検知ロジックが必要な場合はここでパイプラインを組む
        SelectedNode
            .Subscribe(node => 
            {
                if (node != null)
                {
                    // 選択変更時の任意の処理（必要に応じて記述）
                }
            })
            .AddTo(ref _disposables);
    }

    protected override void OnDisposed()
    {
    }
}