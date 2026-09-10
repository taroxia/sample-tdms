// File: Features/History/Models/HistoryItemRecord.cs
using System;

namespace WpfUI.Features.History.Models;

/// <summary>
/// 履歴データの1レコードを表現するドメインモデル
/// </summary>
public sealed record HistoryItemRecord(
    string Id,
    string TestName,
    DateTime MeasuredAt,
    string Category,
    double MaxLoad,          // 最大荷重 [kN]
    double PeakDisplacement, // ピーク変位 [mm]
    bool IsPass,
    string OperatorName,
    string FilePath
);
