using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System.ComponentModel;
using WpfUI.Features.Shell;


namespace WpfUI.Infrastructure.Cli;


public sealed class RunCommand(IServiceProvider provider) : Command<RunCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-g|--gui")]
        [Description("WPF UIモードで起動します")]
        public bool ShowGui { get; init; }
    }

    //public override int Execute(CommandContext context, Settings settings)
    //{


    //    if (settings.ShowGui)
    //    {
    //        var app = new App();
    //        app.InitializeComponent();
    //        return app.Run();
    //    }

    //    AnsiConsole.MarkupLine("[bold green]CLI Mode:[/] Hello from .NET 9 + C# 13!");
    //    return 0;
    //}

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var app = provider.GetRequiredService<App>();
        app.InitializeComponent();
        var mainWindow = provider.GetRequiredService<MainWindow>();

        // app.Run はメインスレッドをブロックし、アプリ終了まで戻りません
        return app.Run(mainWindow);
    }


    //protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    //{
    //    var app = provider.GetRequiredService<App>();
    //    var mainWindow = provider.GetRequiredService<MainWindow>();

    //    // Future-Based: 非同期で初期化が必要な場合はここで await 可能
    //    // await someInitializationService.InitAsync();

    //    //return await Task.Run(() => app.Run(mainWindow));
    //    return await Task.FromResult(app.Run(mainWindow));
    //}
}
