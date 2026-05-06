using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using WpfUI.Core.Abstracts;
using WpfUI.Core.Base;
using WpfUI.Features.Dashboard;
using WpfUI.Features.LiveAnalytics;
using WpfUI.Features.Settings;
using WpfUI.Features.Shell;
using WpfUI.Infrastructure.Cli;
using WpfUI.Infrastructure.Persistence.Tdms;

// ---------------------------------------------------------
// [Package Info]
// - Microsoft.Extensions.Hosting: DI, Logging, Configuration の統合管理.
// - CommunityToolkit.Mvvm: MVVM パターンのボイラープレート削減.
// - ReactiveProperty: 強力なストリーム操作と双方向のデータバインディング.
// - ScottPlot: 
// - Spectre.Console.Cli: 
// ---------------------------------------------------------

namespace WpfUI;

public class Program
{
    [STAThread]
    //public static async Task<int> Main(string[] args)
    public static int Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // サービスの登録 (DI)
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ITdmsService, TdmsService>();

        //ConfigureServices(builder.Services);
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<MainViewModel>();

        builder.Services.AddSingleton<DashboardService>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<DashboardView>();

        builder.Services.AddSingleton<LiveAnalyticsService>();
        builder.Services.AddTransient<LiveAnalyticsViewModel>();
        //        builder.Services.AddTransient<LiveAnalyticsView>();

        builder.Services.AddTransient<SettingsView>();
        builder.Services.AddTransient<SettingsViewModel>();

        var registrar = new TypeRegistrar(builder.Services);
        var app = new CommandApp<RunCommand>(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("MyWpfApp");
        });

        //return await app.RunAsync(args);
        return app.Run(args);

        //host.Start();

        //var app = new App();
        //app.InitializeComponent();

        //// メインウィンドウの解決と実行
        //var mainWindow = host.Services.GetRequiredService<Features.Dashboard.DashboardView>();
        //app.Run(mainWindow);

        //host.StopAsync().GetAwaiter().GetResult();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<App>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<MainViewModel>();

        // Features 以下の View / ViewModel / Service を一括登録
        //services.AddSingleton<Features.Dashboard.DashboardView>();
        //services.AddSingleton<Features.Dashboard.DashboardViewModel>();
    }
}
