// ────────────────────────────────
//
// ────────────────────────────────

using System.IO;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using WpfUI.Core.Base;

namespace WpfUI.Features.Wave.Explorer;

public sealed class WaveExplorerViewModel : ViewModelBase
{
    public ReactivePropertySlim<string> DroppedFilePath { get; } = new(string.Empty);

    public ReactiveCommand<DragEventArgs> DropCommand { get; }
    public ReactiveCommand<DragEventArgs> PreviewDragOverCommand { get; }

    public WaveExplorerViewModel() : base()
    {
        PreviewDragOverCommand = new ReactiveCommand<DragEventArgs>().WithSubscribe(e =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }).AddTo(_disposables);
        DropCommand = new ReactiveCommand<DragEventArgs>().WithSubscribe(async e =>
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                var path = files[0];
                if (Path.GetExtension(path).Equals(".tdms", StringComparison.OrdinalIgnoreCase))
                {
                    DroppedFilePath.Value = path;
                    // 非同期で大容量ファイルを処理
                    //await Task.Run(() => _tdmsService.LoadFile(path));
                }
            }
        });

    }


}
