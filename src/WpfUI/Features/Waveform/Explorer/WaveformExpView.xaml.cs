// ────────────────────────────────
//
// ────────────────────────────────

using System.IO;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using WpfUI.Core.Base;

namespace WpfUI.Features.Waveform.Explorer;

public partial class WaveformExpView : ViewBase<WaveformExpViewModel>
{
    public ReadOnlyReactiveProperty<string> FileName { get; set; }

    public WaveformExpView() : base()
    {
        InitializeComponent();
    }
    protected override void OnViewModelAttached(WaveformExpViewModel? viewModel)
    {
        if (viewModel is null) return;

        //    FileName = viewModel.DroppedFilePath
        //.Select(path => string.IsNullOrEmpty(path) ? "" : Path.GetFileName(path))
        //.ToReadOnlyReactiveProperty().AddTo(viewModel._disposables);

    }
}
