// ────────────────────────────────
//
// ────────────────────────────────

using WpfUI.Core.Collections;
using WpfUI.Core.Domain.Types;
using WpfUI.Infrastructure.Persistence.Tdms;

namespace WpfUI.Core.Abstractions;

public interface ITdmsService
{
    Task<TdmsFileMetadata> GetMetadataAsync(
        string filepath,
        CancellationToken ct = default);

    ValueTask<TdmsData> ReadChannelDataStreamAsync(
        TdmsCacheKey key,
        CancellationToken cancellationToken = default);

    //IAsyncEnumerable<double> ReadChannelDataStreamAsync(
    //    string filePath,
    //    string group,
    //    string channel,
    //    nuint chunkSize = 10000,
    //    CancellationToken ct = default);
}
