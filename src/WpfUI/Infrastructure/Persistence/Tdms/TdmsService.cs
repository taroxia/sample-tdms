using System.Runtime.CompilerServices;
using WpfUI.Core.Abstracts;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Infrastructure.Persistence.Tdms;

public sealed class TdmsService : ITdmsService
{
    // メタデータのみを高速に取得：再帰的にグループとチャンネルを走査
    public async Task<IReadOnlyList<TdmsChannelInfo>> GetMetadataAsync(string filePath, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var file = new TdmsWrapper(filePath);
            file.OpenFile();
            var fInfos = file.GetPropertyInfos();
            //string fName = file.GetFileName();
            var results = new List<TdmsChannelInfo>();

            foreach (var gHandle in file.GetChannelGroups())
            {
                //var gInfos = file.GetPropertyInfos(gHandle);
                // グループ名の取得
                string gName = file.GetChannelGroupName(gHandle);
                foreach (var cHandle in file.GetChannels(gHandle))
                {
                    //var cInfos = file.GetPropertyInfos(cHandle);

                    string cName = file.GetChannelName(cHandle);
                    //string cUnit = file.GetChannelUnit(cHandle);
                    //string cDesc = file.GetChannelDescription(cHandle);

                    ulong numValues = file.GetNumDataValues(cHandle);
                    results.Add(new TdmsChannelInfo(gName, cName, numValues));
                }
            }
            return (IReadOnlyList<TdmsChannelInfo>)results;
        }, ct);
    }

    /*
    // ScottPlot用：全データ取得（バルクリード）
    public async Task<double[]> ReadChannelDataAsync(string filePath, string group, string channel, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var file = new TdmsHandler(filePath);
            if (!TryGetChannelHandle(file.Handle, group, channel, out int chHandle))
                return [];

            TdmNative.DDC_GetNumDataValues(chHandle, out long count);
            var data = new double[count];
            
            // NI DLL の DDC_GetDataValues はスレッドセーフではないため、Task.Run 内で完結させる
            int status = TdmNative.DDC_GetDataValues(chHandle, 0, count, data);
            return status == 0 ? data : [];
        }, ct);
    }
    */

    public async IAsyncEnumerable<double> ReadChannelDataStreamAsync(
        string filePath,
        string group,
        string channel,
        nuint chunkSize = 10000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 処理をスレッドプールに逃がす
        await Task.Yield();

        using var file = new TdmsWrapper(filePath);
        file.OpenFile();
        var chHandle = file.GetChannelByName(group, channel);
        if (chHandle.IsInvalid) { yield break; }

        ulong total = file.GetNumDataValues(chHandle);
        double[] buffer = System.Buffers.ArrayPool<double>.Shared.Rent((int)chunkSize);
        try
        {
            for (nuint offset = 0; offset < total; offset += chunkSize)
            {
                // キャンセル要求の確認
                ct.ThrowIfCancellationRequested();

                ulong remaining = total - offset;
                nuint count = (nuint)Math.Min(chunkSize, remaining);
                ReadToBuffer(file, chHandle, offset, count, buffer);
                for (int i = 0; i < (int)count; i++)
                {
                    yield return buffer[i];
                }
            }
        }
        finally
        {
            System.Buffers.ArrayPool<double>.Shared.Return(buffer);
        }
    }
    private static void ReadToBuffer(TdmsWrapper file, ChannelHandle channel, nuint offset, nuint count, double[] destination)
    {
        using var dataOwner = file.GetDataValues(channel, offset, count);
        dataOwner.Memory.Span.CopyTo(destination);
    }
}
