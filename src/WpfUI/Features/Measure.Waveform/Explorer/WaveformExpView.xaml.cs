// ────────────────────────────────
//
// ────────────────────────────────

using R3;
using WpfUI.Core.Base;

namespace WpfUI.Features.Measure.Waveform.Explorer;

public partial class WaveformExpView : ViewBase<WaveformExpViewModel>
{
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
