using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WpfUI.Infrastructure.Persistence.Parquet;

/// <summary>
/// Parquetファイルへの書き込み専用インターフェース。将来的な拡張を考慮したジェネリック設計。
/// </summary>
public interface IParquetWriter
{
    ValueTask WriteRecordsAsync<TRecord>(
        string filePath,
        IEnumerable<TRecord> records,
        CancellationToken cancellationToken = default) where TRecord : class, new();
}

/// <summary>
/// Parquetファイルからの読み取り専用インターフェース。
/// </summary>
public interface IParquetReader
{
    IAsyncEnumerable<TRecord> ReadRecordsAsync<TRecord>(
        string filePath,
        CancellationToken cancellationToken = default) where TRecord : class, new();
}

public interface IParquetStorageService : IParquetWriter, IParquetReader
{
}
