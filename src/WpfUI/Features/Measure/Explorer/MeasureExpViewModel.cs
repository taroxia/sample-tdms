using System;
using System.Collections.Generic;
using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Measure.Explorer;

public sealed class MeasureExpViewModel : ExplorerViewModelBase
{
    private readonly MeasureService _service;

    public BindableReactiveProperty<string?> SelectedNode => _service.SelectedNode;
    public List<string> AvailableNodes { get; } = ["Node_Alpha_01", "Node_Beta_02", "Node_Gamma_03"];

    public MeasureExpViewModel(MeasureService service)
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