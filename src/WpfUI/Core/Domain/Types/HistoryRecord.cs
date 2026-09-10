// ────────────────────────────────
//
// ────────────────────────────────

using System;

namespace WPFUI.Core.Domain.Types;

public record HistoryRecord(
    Guid Id,
    string TestName,
    string Status,
    DateTime CreatedAt,
    string ParquetFilePath
);
