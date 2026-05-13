// ────────────────────────────────
//
// ────────────────────────────────

using System.IO;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using WpfUI.Core.Base;

namespace WpfUI.Features.Wave.Explorer;

public partial class WaveExplorerView : ViewBase<WaveExplorerViewModel>
{
    public ReadOnlyReactiveProperty<string> FileName { get; set; }

    public WaveExplorerView() : base()
    {
        InitializeComponent();
    }
    protected override void OnViewModelAttached(WaveExplorerViewModel? viewModel)
    {
        if (viewModel is null) return;

        FileName = viewModel.DroppedFilePath
    .Select(path => string.IsNullOrEmpty(path) ? "" : Path.GetFileName(path))
    .ToReadOnlyReactiveProperty().AddTo(viewModel._disposables);

    }
}
