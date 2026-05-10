// ────────────────────────────────
//
// ────────────────────────────────

using WpfUI.Core.Abstracts;

namespace WpfUI.Features.LiveAnalytics;

public sealed class LiveAnalyticsService(ITdmsService tdms)
{
    //public async Task<(double[] Timestamps, double[] Values)> FetchChannelDataAsync(string group, string channel)
    //{
    //    // 実際には nilibdcc.dll を介して ITdmsService が非同期で読み出し
    //    return await tdms.ReadAsync(group, channel);
    //}
    public async Task<double[]> GetPlotDataAsync(string path, string group, string channel, CancellationToken ct)
    {
        // 1. メタデータを取得してデータ量を確認（戦略の決定）
        var metadata = await tdms.GetMetadataAsync(path, ct);
        var targetInfo = metadata.FirstOrDefault(m => m.GroupName == group && m.ChannelName == channel);

        if (targetInfo == null) return [];

        // 2. データ量に応じて読み込み戦略を切り替える
        // 例: 100万点を超える場合は間引き読み込みを行う
        //if (targetInfo.SampleCount > 1_000_000)
        //{
        //    return await _tdms.ReadResampledDataAsync(path, group, channel, targetCount: 5000, ct);
        //}

        // 3. 通常サイズなら非同期ストリームで読み込み、必要に応じて加工
        var result = new List<double>((int)targetInfo.SampleCount);
        await foreach (var value in tdms.ReadChannelDataStreamAsync(path, group, channel, ct: ct))
        {
            // ここでフィルタリングやスケーリングなどの計算（商用ロジック）を挟むことが可能
            result.Add(value);
        }

        return result.ToArray();
    }
}
