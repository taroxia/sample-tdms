// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using R3;

using WpfUI.Core.Abstractions;
using WpfUI.Core.Base;
using WpfUI.Core.Collections;
using WpfUI.Core.Dmain.Models;

namespace WpfUI.Features.Waveform.Explorer;

/// <summary>
/// ViewModel for the TDMS File/Channel Explorer interface supporting Drag and Drop.
/// </summary>
public sealed class WaveformExpViewModel : ViewModelBase
{
    private readonly WaveformService _service;
    private readonly ITdmsService _tdms;

    private Point _dragStartPoint;
    private bool _isTrackingMouse;
    private ChannelNodeViewModel? _draggedChannel;

    // ----------------------------------------------------------------
    // Properties / Commands
    // ----------------------------------------------------------------

    public ReactiveProperty<string> SearchText { get; } = new(string.Empty);
    public ReadOnlyReactiveProperty<bool> IsEmpty { get; }

    public ObservableCollection<IExplorerNodeViewModel> FlattenedNodes { get; } = new();

    public ReactiveCommand<DragEventArgs> DropFileCommand { get; } = new();
    public ReactiveCommand<IExplorerNodeViewModel> DeleteFileCommand { get; } = new();
    public ReactiveCommand<IExplorerNodeViewModel> CopyPropertyCommand { get; } = new();
    public ReactiveCommand<MouseButtonEventArgs> ToggleAllSelectCommand { get; } = new();

    public ReactiveCommand<MouseButtonEventArgs> ChannelMouseDownCommand { get; } = new();
    public ReactiveCommand<MouseEventArgs> ChannelMouseMoveCommand { get; } = new();
    public ReactiveCommand<MouseButtonEventArgs> ChannelMouseUpCommand { get; } = new();

    // ----------------------------------------------------------------
    // Constructor
    // ----------------------------------------------------------------

    public WaveformExpViewModel(WaveformService service, ITdmsService tdms)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tdms = tdms ?? throw new ArgumentNullException(nameof(tdms));

        // Setup reactive UI states
        IsEmpty = _service.LoadedFiles
            .Select(files => files == null || files.Count == 0)
            .ToReadOnlyReactiveProperty()
            .AddTo(ref _disposables);

        InitializeCommandSubscriptions();
    }

    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializeCommandSubscriptions()
    {
        // Handle file drop sequence securely via R3 pipeline
        DropFileCommand
            .Where(e => e.Data.GetDataPresent(DataFormats.FileDrop))
            .Select(e => e.Data.GetData(DataFormats.FileDrop) as string[])
            .Where(files => files != null && files.Length > 0)
            .Subscribe(async files =>
            {
                try
                {
                    await _service.LoadTdmsFilesAsync(files!);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Error] File drop execution failed: {ex.Message}");
                }
            })
            .AddTo(ref _disposables);

        // Toggle selection state for all channels in a group
        ToggleAllSelectCommand
            .Subscribe(args =>
            {
                if (args.OriginalSource is not FrameworkElement element ||
                    element.DataContext is not GroupNodeViewModel group) return;

                args.Handled = true;

                var targetKeys = group.Children.Select(channel => channel.Metadata.CacheKey);
                var shouldSelect = !group.IsSelected.Value;
                if (shouldSelect)
                {
                    _service.AddSelection(targetKeys);
                }
                else
                {
                    _service.RemoveSelection(targetKeys);
                }
            })
            .AddTo(ref _disposables);

        // Process pure click toggle actions on mouse up
        ChannelMouseUpCommand
            .Subscribe(e =>
            {
                if (!_isTrackingMouse) return;

                if (_draggedChannel is not null)
                {
                    if (!_draggedChannel.IsSelected.Value)
                        _service.AddSelection(_draggedChannel.Metadata.CacheKey);
                    else
                        _service.RemoveSelection(_draggedChannel.Metadata.CacheKey);

                    e.Handled = true;
                }
                ResetDragState();
            })
            .AddTo(ref _disposables);

        // Capture initial position for Drag and Drop threshold evaluation
        ChannelMouseDownCommand
            .Subscribe(e =>
            {
                if (e.OriginalSource is DependencyObject depObj &&
                    FindAncestor<FrameworkElement>(depObj) is FrameworkElement element &&
                    element.DataContext is ChannelNodeViewModel channelViewModel)
                {
                    _dragStartPoint = e.GetPosition(null);
                    _isTrackingMouse = true;
                    _draggedChannel = channelViewModel;
                    e.Handled = true;
                }
            })
            .AddTo(ref _disposables);

        // Evaluate and trigger system Drag and Drop operations
        ChannelMouseMoveCommand
            .Subscribe(e =>
            {
                if (!_isTrackingMouse || _draggedChannel is null) return;

                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    ResetDragState();
                    return;
                }

                var currentPosition = e.GetPosition(null);
                var diff = _dragStartPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (e.Source is DependencyObject dragSource)
                    {
                        _isTrackingMouse = false;

                        var dataObject = new DataObject("ArcticSlate.TdmsChannel", _draggedChannel.Metadata);
                        DragDrop.DoDragDrop(dragSource, dataObject, DragDropEffects.Copy);

                        ResetDragState();
                    }
                }
            })
            .AddTo(ref _disposables);

        // Handle file node removal
        DeleteFileCommand
            .Subscribe(node =>
            {
                if (node is FileNodeViewModel fileNode)
                {
                    _service.RemoveTdmsFiles(new[] { fileNode.FilePath });
                }
            })
            .AddTo(ref _disposables);

        // Route property copying command execution
        CopyPropertyCommand
            .Subscribe(OnCopyProperty)
            .AddTo(ref _disposables);

        // Sync visual selections from external service events
        _service.SelectedExpChannelsChanged
            .Subscribe(UpdateNodeSelectionVisuals)
            .AddTo(ref _disposables);

        // Synchronize and re-build tree hierarchy automatically on data changes
        _service.LoadedFiles
            .ObserveOnCurrentDispatcher()
            .Where(files => files != null)
            .CombineLatest(SearchText, (files, query) => (files, query))
            .Subscribe(x => BuildExplorerTree(x.files, x.query))
            .AddTo(ref _disposables);
    }

    // ----------------------------------------------------------------
    // Public Logic Methods
    // ----------------------------------------------------------------

    /// <summary>
    /// Constructs a filtered visual tree model representation of the loaded TDMS data structure.
    /// </summary>
    public void BuildExplorerTree(IReadOnlyList<TdmsFileMetadata> files, string query)
    {
        FlattenedNodes.Clear();
        if (files.Count == 0) return;

        var filter = query ?? string.Empty;
        var isFilterEmpty = string.IsNullOrEmpty(filter);

        foreach (var file in files)
        {
            var fileNode = new FileNodeViewModel(file);
            var visibleGroups = new List<GroupNodeViewModel>();

            foreach (var group in file.Groups)
            {
                var groupNode = new GroupNodeViewModel(group, fileNode);

                var matchedChannels = group.Channels
                    .Where(ch => isFilterEmpty || ch.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .Select(ch =>
                    {
                        var chNode = new ChannelNodeViewModel(ch, groupNode);
                        chNode.IsSelected.Value = _service.GetSelectedChannels().Contains(ch.CacheKey);
                        return chNode;
                    })
                    .ToList();

                if (matchedChannels.Count > 0 || isFilterEmpty)
                {
                    foreach (var chNode in matchedChannels)
                    {
                        groupNode.AddChild(chNode);
                    }

                    if (matchedChannels.Count > 0 && matchedChannels.All(ch => ch.IsSelected.Value))
                    {
                        groupNode.IsSelected.Value = true;
                    }
                    visibleGroups.Add(groupNode);
                }
            }

            if (visibleGroups.Count > 0)
            {
                FlattenedNodes.Add(fileNode);
                foreach (var groupNode in visibleGroups)
                {
                    fileNode.AddChild(groupNode);
                    FlattenedNodes.Add(groupNode);
                }
            }
        }
    }

    // ----------------------------------------------------------------
    // Private Helper Methods
    // ----------------------------------------------------------------

    private void UpdateNodeSelectionVisuals(IReadOnlySet<TdmsCacheKey> activeKeys)
    {
        foreach (var groupNode in FlattenedNodes.OfType<GroupNodeViewModel>())
        {
            var channels = groupNode.Children.OfType<ChannelNodeViewModel>().ToList();

            foreach (var chNode in channels)
            {
                chNode.IsSelected.Value = activeKeys.Contains(chNode.Metadata.CacheKey);
            }
            groupNode.IsSelected.Value = channels.Count > 0 && channels.All(ch => ch.IsSelected.Value);
        }
    }

    private void OnCopyProperty(IExplorerNodeViewModel? node)
    {
        if (node is ChannelNodeViewModel channelNode)
        {
            var props = string.Join(Environment.NewLine,
                channelNode.Metadata.Properties.Select(p => $"{p.Key}: {p.Value}"));

            Clipboard.SetText($"[Channel: {channelNode.Name}]{Environment.NewLine}{props}");
        }
    }

    private void ResetDragState()
    {
        _isTrackingMouse = false;
        _draggedChannel = null;
    }

    // Recursively traverses up the visual tree to find a matching ancestor type.
    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T ancestor) return ancestor;
            current = VisualTreeHelper.GetParent(current);
        } while (current != null);

        return null;
    }
}
