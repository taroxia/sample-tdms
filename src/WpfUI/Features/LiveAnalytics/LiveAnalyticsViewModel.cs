using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ScottPlot;
using System.Reactive.Linq;
using System.Windows;
using WpfUI.Core.Base;

namespace WpfUI.Features.LiveAnalytics;

public sealed class LiveAnalyticsViewModel(LiveAnalyticsService service) : ViewModelBase
{
    // 1. 状態管理: 変更通知が必要な内部状態
    private readonly ReactivePropertySlim<bool> _isLoading = new(false);

    // 2. View公開用: Converterを通さず、ViewModel側でViewの型に責務を持つ
    public ReadOnlyReactivePropertySlim<Visibility> IsBusyVisibility => _isLoading
        .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
        .ToReadOnlyReactivePropertySlim();

    // 3. コマンド: ロード中は自動実行不可 + 実行ロジックを直接バインド
    public AsyncReactiveCommand LoadDataCommand => _isLoading
        .Inverse() // x => !x のショートカット
        .ToAsyncReactiveCommand()
        .WithSubscribe(async () =>
        {
            // WithSubscribe を使うことで Subscribe 登録をインライン化
            _isLoading.Value = true;
            try
            {
                await LoadAsync();
            }
            finally
            {
                _isLoading.Value = false;
            }
        })
        .AddTo(_disposables); // ViewModelBase継承のCompositeDisposableへ
    public ReactivePropertySlim<Plot?> PlotInstance { get; } = new();

    private async Task LoadAsync()
    {

        string path = "E:\\work\\project\\env\\exampleMeasurements.tdms";
        string group = "EH";
        string channel = "VaGround";

        double[] data = await service.GetPlotDataAsync(path, group, channel, CancellationToken.None);

        //await Task.Delay(1000);
        ////await service.LoadTdmsDataAsync(); // サービス層の呼び出し

        //var (t, v) = await service.FetchChannelDataAsync("Group1", "Ch1");

        //// ScottPlot 5 のプロット設定
        var newPlot = new Plot();
        //newPlot.Add.SignalXY(t, v);


        double[] dataX = { 1, 2, 3, 4, 5 };
        double[] dataY = { 1, 4, 9, 16, 25 };
        newPlot.Add.SignalXY(dataX, dataY);


        //newPlot.Plot.Add.Scatter(dataX, dataY);

        newPlot.Title("TDMS High-Speed Analysis");
        newPlot.Axes.AutoScale();

        PlotInstance.Value = newPlot;
    }



    /*
    private readonly ReactivePropertySlim<bool> _isLoading = new(false);

    // 2. View公開用：ToVisibility() で Visibility型に直接変換 (ReadOnly)
    public ReadOnlyReactivePropertySlim<Visibility> IsBusyVisibility => _isLoading
        .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
            //            .ToVisibility()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

    // 3. コマンド：ロード中は自動で実行不可になる
    public AsyncReactiveCommand LoadDataCommand => _isLoading
        .Inverse() // !x の糖衣構文
        .ToAsyncReactiveCommand()
        .AddTo(_disposables);
    public ReadOnlyReactiveProperty<int> Age { get; }

    init
        {
            Age = _age.ToReadOnlyReactiveProperty();
            //LoadDataCommand.Subscribe(async _ =>
            //{
            //    using (_isLoading.ProcessStart()) // 拡張メソッド自作を推奨
            //    {
            //        await Task.Delay(1000);
            //        // await analyticsService.LoadAsync(); // 引数を直接使用可能
            //    }
            //}).AddTo(_disposables);
        }
    */

}
