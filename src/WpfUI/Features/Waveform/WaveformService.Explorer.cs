// ────────────────────────────────
//
// ────────────────────────────────

using R3;

using WpfUI.Core.Collections;
using WpfUI.Core.Dmain.Models;

namespace WpfUI.Features.Waveform;

/// <summary>
/// Provides high-performance management for TDMS file metadata and channel selection states.
/// </summary>
public sealed partial class WaveformService
{
    // ----------------------------------------------------------------
    // Properties / Commands
    // ----------------------------------------------------------------

    private readonly HashSet<TdmsCacheKey> _selectedExpChannelKeys = new();
    private readonly Subject<IReadOnlySet<TdmsCacheKey>> _selectedExpChannelsChanged = new();

    public Observable<IReadOnlySet<TdmsCacheKey>> SelectedExpChannelsChanged => _selectedExpChannelsChanged;
    public ReactiveProperty<IReadOnlyList<TdmsFileMetadata>> LoadedFiles { get; } = new([]);

    public ReadOnlyReactiveProperty<Dictionary<TdmsCacheKey, TdmsChannelMetadata>> SelectedExpChannelsMetadata { get; private set; } = null!;
    private ReadOnlyReactiveProperty<Dictionary<TdmsCacheKey, TdmsChannelMetadata>> _masterChannelLookup = null!;

    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializeExplorerPipeline()
    {
        _masterChannelLookup = LoadedFiles
            .Select(files =>
            {
                var dict = new Dictionary<TdmsCacheKey, TdmsChannelMetadata>();
                if (files == null) return dict;

                foreach (var file in files)
                {
                    if (file.ChannelLookup == null) continue;
                    foreach (var kvp in file.ChannelLookup)
                    {
                        dict[kvp.Key] = kvp.Value;
                    }
                }
                return dict;
            })
            .ToReadOnlyReactiveProperty(new Dictionary<TdmsCacheKey, TdmsChannelMetadata>())
            .AddTo(ref _disposables);

        SelectedExpChannelsMetadata = Observable
            .CombineLatest(
                _masterChannelLookup,
                _selectedExpChannelsChanged,
                (lookup, keys) =>
                {
                    var result = new Dictionary<TdmsCacheKey, TdmsChannelMetadata>();
                    if (keys == null || lookup.Count == 0) return result;

                    foreach (var key in keys)
                    {
                        if (lookup.TryGetValue(key, out var metadata))
                        {
                            result[key] = metadata;
                        }
                    }
                    return result;
                })
            .ToReadOnlyReactiveProperty(new Dictionary<TdmsCacheKey, TdmsChannelMetadata>())
            .AddTo(ref _disposables);
    }

    // ----------------------------------------------------------------
    // Public Logic Methods
    // ----------------------------------------------------------------

    /// <summary>
    /// Asynchronously parses and loads metadata from multiple TDMS files in parallel.
    /// </summary>
    public async Task LoadTdmsFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default)
    {
        if (filePaths is null) return;

        var targetPaths = filePaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && path.EndsWith(".tdms", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (targetPaths.Length == 0) return;

        HashSet<string> existingPaths;
        lock (_lock)
        {
            existingPaths = LoadedFiles.Value.Select(m => m.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var newPaths = targetPaths.Where(path => !existingPaths.Contains(path)).ToArray();
        if (newPaths.Length == 0) return;

        var tasks = newPaths.Select(async path =>
        {
            try
            {
                return await _tdmsService.GetMetadataAsync(path, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error] TDMS Parsing Failed ({path}): {ex.Message}");
                return null;
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var validMetadata = results.Where(m => m is not null).Cast<TdmsFileMetadata>().ToArray();
        if (validMetadata.Length == 0) return;

        lock (_lock)
        {
            var updatedList = new List<TdmsFileMetadata>(LoadedFiles.Value);
            updatedList.AddRange(validMetadata);
            LoadedFiles.Value = updatedList;
        }
    }

    /// <summary>
    /// Removes specified TDMS files and evicts their associated raw data from the LRU cache.
    /// </summary>
    public void RemoveTdmsFiles(IEnumerable<string> filePaths)
    {
        if (filePaths is null) return;

        var targets = filePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (targets.Count == 0) return;

        lock (_lock)
        {
            var currentList = LoadedFiles.Value;
            if (!currentList.Any(m => targets.Contains(m.FilePath))) return;

            var updatedList = currentList.Where(m => !targets.Contains(m.FilePath)).ToList();

            foreach (var fileMetadata in currentList.Where(m => targets.Contains(m.FilePath)))
            {
                foreach (var group in fileMetadata.Groups)
                {
                    foreach (var channel in group.Channels)
                    {
                        _selectedExpChannelKeys.Remove(channel.CacheKey);
                        _cache.Remove(channel.CacheKey); // LRU Cashe.
                    }
                }
            }

            LoadedFiles.Value = updatedList;
            NotifyChange();
        }
    }

    public void AddSelection(TdmsCacheKey key)
    {
        lock (_lock)
        {
            if (_selectedExpChannelKeys.Add(key)) { NotifyChange(); }
        }
    }
    public void AddSelection(IEnumerable<TdmsCacheKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        bool isChanged = false;
        lock (_lock)
        {
            foreach (var key in keys)
            {
                if (_selectedExpChannelKeys.Add(key))
                {
                    isChanged = true;
                }
            }
            if (isChanged) { NotifyChange(); }
        }
    }

    public void RemoveSelection(TdmsCacheKey key)
    {
        lock (_lock)
        {
            if (_selectedExpChannelKeys.Remove(key)) { NotifyChange(); }
        }
    }
    public void RemoveSelection(IEnumerable<TdmsCacheKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        bool isChanged = false;
        lock (_lock)
        {
            foreach (var key in keys)
            {
                if (_selectedExpChannelKeys.Remove(key))
                {
                    isChanged = true;
                }
            }
            if (isChanged) { NotifyChange(); }
        }
    }

    public void ClearExpSelection()
    {
        lock (_selectedExpChannelKeys)
        {
            if (_selectedExpChannelKeys.Count > 0)
            {
                _selectedExpChannelKeys.Clear();
                NotifyChange();
            }
        }
    }

    public IReadOnlySet<TdmsCacheKey> GetSelectedChannels()
    {
        lock (_lock)
        {
            return new HashSet<TdmsCacheKey>(_selectedExpChannelKeys);
        }
    }

    // ----------------------------------------------------------------
    // Private Helper Methods
    // ----------------------------------------------------------------

    private void NotifyChange() =>
        _selectedExpChannelsChanged.OnNext(new HashSet<TdmsCacheKey>(_selectedExpChannelKeys));
}
