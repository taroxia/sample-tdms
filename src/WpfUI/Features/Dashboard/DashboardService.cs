using WpfUI.Core.Abstracts;

namespace WpfUI.Features.Dashboard;

public sealed class DashboardService(ITdmsService tdmsService)
{
    private readonly ITdmsService _tdms = tdmsService;
    // C# 13 では params IEnumerable 等の改善があるが、ここはシンプルに Task で
    public async Task<DashboardData> GetStatusAsync()
    {
        await Task.Delay(500); // 通信シミュレーション
        return new DashboardData(System.DateTime.Now, "正常稼働中", 85);
    }
}

public record DashboardData(DateTime LastUpdate, string Status, int CpuUsage);
