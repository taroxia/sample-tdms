// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using R3;
using WpfUI.Core.Abstracts;
using WpfUI.Core.Base;

namespace WpfUI.Features.Waveform.Explorer;

public sealed class WaveformExpViewModel : ViewModelBase
{
    private readonly ITdmsService _tdmsService;

    public ObservableCollection<TdmsChannelInfo> Channels { get; } = [];
    public BindableReactiveProperty<bool> IsEmpty { get; }

    public ReactiveCommand<DragEventArgs> DropFileCommand { get; } = new();
    public ReactiveCommand<MouseEventArgs> StartDragCommand { get; } = new();
    public ReactiveCommand<System.Collections.IList> SelectionChangedCommand { get; } = new();

    // 選択されたチャネルを外部（WaveformView）へ通知するための ReactiveProperty
    public ReactiveProperty<IEnumerable<TdmsChannelInfo>> SelectedChannels { get; } = new();

    public WaveformExpViewModel(ITdmsService tdmsService)
    {
        _tdmsService = tdmsService;

        var countChanged =
            Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    h => (s, e) => h(e),
                    h => Channels.CollectionChanged += h,
                    h => Channels.CollectionChanged -= h)
                .Select(_ => Channels.Count == 0)
                .Prepend(Channels.Count == 0);
        IsEmpty =
            countChanged
                .ToBindableReactiveProperty()
                .AddTo(_disposables);

        DropFileCommand.SubscribeAwait(async (e, ct) => await OnDropFileCommand(e, ct));
        StartDragCommand.SubscribeAwait(async (e, ct) => await OnStartDragCommand(e, ct));

        //SelectionChangedCommand = new ReactiveCommand<System.Collections.IList>();
        SelectionChangedCommand.Subscribe(items =>
        {
            SelectedChannels.Value = items.Cast<TdmsChannelInfo>().ToList();
        });
    }

    private async Task OnDropFileCommand(DragEventArgs e, CancellationToken ct)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            foreach (var file in files.Where(f => f.EndsWith(".tdms", StringComparison.OrdinalIgnoreCase)))
            {
                var metadata = await _tdmsService.GetMetadataAsync(file);
                foreach (var info in metadata) Channels.Add(info);
            }
        }
    }

    private async Task OnStartDragCommand(MouseEventArgs e, CancellationToken ct)
    {
        if (e.LeftButton == MouseButtonState.Pressed && SelectedChannels.Value?.Any() == true)
        {
            var target = e.Source as FrameworkElement;

            // 転送するデータを作成 (List<TdmsChannelInfo> としてパッケージ)
            var dataObject = new DataObject();
            dataObject.SetData(typeof(List<TdmsChannelInfo>), SelectedChannels.Value.ToList());

            // D&D実行 (ブロック処理なので注意)
            DragDrop.DoDragDrop(target!, dataObject, DragDropEffects.Copy);
        }
    }
}
/*
public class TdmsNodeViewModel(string name, string iconKey, int depth, TdmsChannelInfo? raw = null)
{
    public string Name { get; } = name;
    public string IconKey { get; } = iconKey;
    public int Depth { get; } = depth;
    public TdmsChannelInfo? RawData { get; } = raw;
    public ObservableCollection<TdmsNodeViewModel> Children { get; } = new();
    public ReactivePropertySlim<bool> IsExpanded { get; } = new(true);
}

// 階層データ構造 (C# 13 Primary Constructor)
public record TdmsFileNode(string Name, List<TdmsGroupNode> Groups);
public record TdmsGroupNode(string Name, List<TdmsChannelNode> Channels);
public class TdmsChannelNode(TdmsChannelInfo info)
{
    public string Name => info.ChannelName;
    public string Detail => $"{info.SampleCount:N0} pts | {info.DataType}";
    public ReactiveProperty<bool> IsSelected { get; } = new(false);
}
*/

/*
    public ReactivePropertySlim<string> DroppedFilePath { get; } = new(string.Empty);

    public ReactiveCommand<DragEventArgs> DropCommand { get; }
    public ReactiveCommand<DragEventArgs> PreviewDragOverCommand { get; }

    public WaveformExpViewModel() : base()
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
*/


//public class TdmsNodeViewModel(string name, ulong? samples = null, bool isFile = false, bool isGroup = false)
//{
//    public string Name { get; } = name;
//    public string Info => samples.HasValue ? $"{samples:N0} pts" : "";
//    public ObservableCollection<TdmsNodeViewModel> Children { get; } = new();

//    public Geometry? Icon => Application.Current.TryFindResource(GetIconKey()) as Geometry;

//    private string GetIconKey() => (isFile, isGroup) switch
//    {
//        (true, _) => "IconFileTdms",
//        (_, true) => "IconFolder",
//        _ => "IconWaveform"
//    };

//    public Brush IconColor => isFile ? Brushes.SkyBlue : isGroup ? Brushes.Gold : Brushes.LimeGreen;
//}
