// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using R3;
using WpfUI.Core.Base;
using WpfUI.Features.History.Models;

namespace WpfUI.Features.History.Explorer;

public sealed class HistoryExpViewModel : ExplorerViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly List<HistoryItemRecord> _masterRecords;

    // --- 検索・フィルタープロパティ (R3) ---
    public ReactiveProperty<string> SearchText { get; } = new("");
    public ReactiveProperty<string> SelectedCategory { get; } = new("すべて");
    public ReactiveProperty<string> SelectedStatus { get; } = new("すべて");
    public ReactiveProperty<DateTime?> StartDate { get; } = new(DateTime.Today.AddDays(-30));
    public ReactiveProperty<DateTime?> EndDate { get; } = new(DateTime.Today);

    // フィルター選択肢
    public IReadOnlyList<string> Categories { get; } = ["すべて", "引張試験", "圧縮試験", "疲労試験", "曲げ試験"];
    public IReadOnlyList<string> Statuses { get; } = ["すべて", "PASS", "FAIL"];

    // --- フィルタリング結果 ＆ 選択状態 ---
    public BindableReactiveProperty<IReadOnlyList<HistoryItemRecord>> FilteredItems { get; }
    public ObservableCollection<HistoryItemRecord> SelectedItems { get; } = [];

    public BindableReactiveProperty<int> TotalCount { get; } = new(0);
    public BindableReactiveProperty<int> SelectedCount { get; } = new(0);

    // --- コマンド (R3 ReactiveCommand) ---
    public ReactiveCommand<Unit> ResetFilterCommand { get; }
    public ReactiveCommand<Unit> OpenSelectedInDockCommand { get; }
    public ReactiveCommand<HistoryItemRecord> OpenSingleInDockCommand { get; }

    public HistoryExpViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        // 仮データセット生成 (35件)
        _masterRecords = GenerateMockData();

        // R3: 5つのフィルター条件をリアルタイム合成してフィルタリング実行
        FilteredItems = Observable.CombineLatest(
            SearchText,
            SelectedCategory,
            SelectedStatus,
            StartDate,
            EndDate,
            FilterRecords)
            .ToBindableReactiveProperty(_masterRecords);

        // 件数更新の同期
        FilteredItems
            .Subscribe(items => TotalCount.Value = items.Count)
            .AddTo(ref _disposables);

        // フィルター条件のリセットコマンド
        ResetFilterCommand = new ReactiveCommand<Unit>(_ =>
        {
            SearchText.Value = "";
            SelectedCategory.Value = "すべて";
            SelectedStatus.Value = "すべて";
            StartDate.Value = DateTime.Today.AddDays(-30);
            EndDate.Value = DateTime.Today;
        }).AddTo(ref _disposables);

        // 選択項目の右下ドッキング領域への一括投下コマンド (1以上選択時のみ実行可)
        OpenSelectedInDockCommand = SelectedCount
            .Select(count => count > 0)
            .ToReactiveCommand(_ =>
            {
                foreach (var item in SelectedItems.ToList())
                {
                    DispatchToDockingArea(item);
                }
            })
            .AddTo(ref _disposables);

        // 単一項目の右下ドッキング領域への即時投下コマンド (1-Click)
        OpenSingleInDockCommand = new ReactiveCommand<HistoryItemRecord>(item =>
        {
            if (item != null)
            {
                DispatchToDockingArea(item);
            }
        }).AddTo(ref _disposables);
    }

    /// <summary>
    /// 条件に基づく多角的なレコード絞り込み
    /// </summary>
    private IReadOnlyList<HistoryItemRecord> FilterRecords(
        string query, string category, string status, DateTime? start, DateTime? end)
    {
        IEnumerable<HistoryItemRecord> results = _masterRecords;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            results = results.Where(r =>
                r.Id.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.TestName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.OperatorName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (category != "すべて")
        {
            results = results.Where(r => r.Category == category);
        }

        if (status != "すべて")
        {
            bool isPass = status == "PASS";
            results = results.Where(r => r.IsPass == isPass);
        }

        if (start.HasValue)
        {
            results = results.Where(r => r.MeasuredAt >= start.Value.Date);
        }

        if (end.HasValue)
        {
            var endOfDay = end.Value.Date.AddDays(1).AddTicks(-1);
            results = results.Where(r => r.MeasuredAt <= endOfDay);
        }

        return results.ToList();
    }

    /// <summary>
    /// パターンBアーキテクチャ: 右下共有ドッキング領域へ波形ドキュメントを即座に配置
    /// </summary>
    private void DispatchToDockingArea(HistoryItemRecord item)
    {
        // INavigationService.Documents コレクションへ追加することで
        // 右下の AvalonDock 領域に新しい波形比較ドキュメントが動的生成される
        // ※ 実際のドキュメントVMインスタンス生成ロジックと連携
    }

    /// <summary>
    /// C# 13 構文を活用したモックデータソース生成
    /// </summary>
    private static List<HistoryItemRecord> GenerateMockData()
    {
        string[] categories = ["引張試験", "圧縮試験", "疲労試験", "曲げ試験"];
        string[] operators = ["佐藤 健", "田中 裕子", "鈴木 一朗", "高橋 誠"];
        var random = new Random(42);

        var list = new List<HistoryItemRecord>();
        var now = DateTime.Now;

        for (int i = 1; i <= 35; i++)
        {
            var category = categories[random.Next(categories.Length)];
            var isPass = random.NextDouble() > 0.12; // 88% PASS
            var date = now.AddDays(-random.Next(0, 40)).AddMinutes(-random.Next(0, 1440));
            var load = Math.Round(12.5 + random.NextDouble() * 80.0, 2);
            var disp = Math.Round(1.2 + random.NextDouble() * 10.0, 3);
            var op = operators[random.Next(operators.Length)];

            list.Add(new HistoryItemRecord(
                Id: $"REC-{20260000 + i}",
                TestName: $"{category}_ロット{100 + (i % 5):D3}",
                MeasuredAt: date,
                Category: category,
                MaxLoad: load,
                PeakDisplacement: disp,
                IsPass: isPass,
                OperatorName: op,
                FilePath: $@"C:\Data\History\2026\REC-{20260000 + i}.tdms"
            ));
        }

        return [.. list.OrderByDescending(x => x.MeasuredAt)];
    }
}
