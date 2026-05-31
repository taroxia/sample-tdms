// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfUI.Core.Abstractions;
using WpfUI.Core.Base;
using WpfUI.Core.Collections;
using WpfUI.Core.Dmain.Models;
using WpfUI.Infrastructure.Persistence.Tdms;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Features.Waveform;

public sealed partial class WaveformService
{
    // ----------------------------------------------------------------
    // Properties / Commands
    // ----------------------------------------------------------------

    private readonly LruCache<TdmsCacheKey, TdmsData> _cache = new(capacity: 20);

    // ----------------------------------------------------------------
    // Pipeline Initialization
    // ----------------------------------------------------------------

    private void InitializeGraphPipeline()
    { }

    // ----------------------------------------------------------------
    // Public Logic Methods
    // ----------------------------------------------------------------

    public async ValueTask<CacheLease<TdmsData>> GetChannelDataAsync(TdmsChannelMetadata channel, CancellationToken ct = default)
    {
        return await _cache.GetOrAddAsync(channel.CacheKey, async key =>
        {
            return await _tdmsService.ReadChannelDataStreamAsync(key, ct);
        }).ConfigureAwait(false);
    }
    public void ClearCache()
    {
        _cache.Clear();
    }

    // ----------------------------------------------------------------
    // Private Helper Methods
    // ----------------------------------------------------------------
}
