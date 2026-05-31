// ────────────────────────────────
//
// ────────────────────────────────

using R3;

using WpfUI.Core.Base;
using WpfUI.Core.Dmain.Models;

namespace WpfUI.Features.Waveform.Explorer;

/// <summary>
/// Defines the hierarchical contract for TDMS metadata explorer nodes.
/// </summary>
public interface IExplorerNodeViewModel
{
    string Name { get; }
    BindableReactiveProperty<bool> IsSelected { get; }
    IEnumerable<IExplorerNodeViewModel> Children { get; }
}

/// <summary>
/// Represents a TDMS file level node in the explorer tree.
/// </summary>
public sealed class FileNodeViewModel : ViewModelBase, IExplorerNodeViewModel
{
    private readonly List<GroupNodeViewModel> _children = [];

    public string Name { get; }
    public string FilePath { get; }
    public BindableReactiveProperty<bool> IsSelected { get; } = new();
    public IEnumerable<GroupNodeViewModel> Children => _children;

    IEnumerable<IExplorerNodeViewModel> IExplorerNodeViewModel.Children => _children;

    public FileNodeViewModel(TdmsFileMetadata metadata)
    {
        Name = metadata.Name;
        FilePath = metadata.FilePath;
        IsSelected.AddTo(ref _disposables);
    }

    public void AddChild(GroupNodeViewModel child) => _children.Add(child);
}

/// <summary>
/// Represents a TDMS group level node containing multiple channels.
/// </summary>
public sealed class GroupNodeViewModel : ViewModelBase, IExplorerNodeViewModel
{
    private readonly List<ChannelNodeViewModel> _children = [];

    public string Name { get; }
    public string Info => $"[{_children.Count} Chs]";
    public BindableReactiveProperty<bool> IsSelected { get; } = new();
    public IEnumerable<ChannelNodeViewModel> Children => _children;
    public FileNodeViewModel File { get; }

    IEnumerable<IExplorerNodeViewModel> IExplorerNodeViewModel.Children => _children;

    public GroupNodeViewModel(TdmsGroupMetadata metadata, FileNodeViewModel file)
    {
        Name = metadata.Name;
        File = file;
        IsSelected.AddTo(ref _disposables);
    }

    public void AddChild(ChannelNodeViewModel child) => _children.Add(child);
}

/// <summary>
/// Represents a TDMS channel level node with explicit data specifications.
/// </summary>
public sealed class ChannelNodeViewModel : ViewModelBase, IExplorerNodeViewModel
{
    public string Name => Metadata.Name;
    public string Info => $"{Metadata.SampleCount:N0} pts ({Metadata.DataType})";
    public BindableReactiveProperty<bool> IsSelected { get; } = new();
    public TdmsChannelMetadata Metadata { get; }
    public GroupNodeViewModel Group { get; }

    IEnumerable<IExplorerNodeViewModel> IExplorerNodeViewModel.Children => Array.Empty<IExplorerNodeViewModel>();

    public ChannelNodeViewModel(TdmsChannelMetadata metadata, GroupNodeViewModel group)
    {
        Metadata = metadata;
        Group = group;
        IsSelected.AddTo(ref _disposables);
    }
}
