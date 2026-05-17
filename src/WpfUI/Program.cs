// ────────────────────────────────
//
// ────────────────────────────────

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using WpfUI.Core.Abstracts;
using WpfUI.Core.Base;
using WpfUI.Features.Settings;
using WpfUI.Features.Shell;
using WpfUI.Features.Waveform;
using WpfUI.Features.Waveform.Explorer;
using WpfUI.Infrastructure.Cli;
using WpfUI.Infrastructure.Persistence.Tdms;

// ---------------------------------------------------------
// [Package Info]
// - Microsoft.Extensions.Hosting: DI, Logging, Configuration の統合管理.
// - CommunityToolkit.Mvvm: MVVM パターンのボイラープレート削減.
// - R3.ReactiveProperty: 強力なストリーム操作と双方向のデータバインディング.
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
        //builder.Services.AddSingleton<IWaveformStateService, WaveformStateService>();
        builder.Services.AddSingleton<WaveformStateService>();

        // --- Navigation Mapping (The Source of Truth) ---
        builder.Services.AddNavigation(nav => nav
            .Add<WaveformView, WaveformViewModel,
                 WaveformExpView, WaveformExpViewModel>("Wave", "Icon.Waveform")
            .Add<SettingsView, SettingsViewModel>("Settings", "Icon.Settings")
        );

        //ConfigureServices(builder.Services);
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<MainViewModel>();

        var registrar = new TypeRegistrar(builder.Services);
        var app = new CommandApp<RunCommand>(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("WpfUI");
        });

        //return await app.RunAsync(args);
        return app.Run(args);
    }
}
