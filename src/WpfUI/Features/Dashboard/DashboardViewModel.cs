using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfUI.Features.Dashboard;

public sealed class DashboardViewModel(DashboardService service) : ObservableObject
{
    public DashboardViewModel() : this(null!) { }

    private string _status = "Loading...";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private int _cpuUsage;
    public int CpuUsage
    {
        get => _cpuUsage;
        set => SetProperty(ref _cpuUsage, value);
    }

    public async Task InitializeAsync()
    {
        var data = await service.GetStatusAsync();
        Status = $"{data.Status} ({data.LastUpdate:HH:mm:ss})";
        CpuUsage = data.CpuUsage;
    }
}
