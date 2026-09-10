// ────────────────────────────────
//
// ────────────────────────────────

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Parquet.Serialization; // Parquet.Net 6 のシリアライザ名前空間
using WpfUI.Core.Base;

namespace WpfUI.Infrastructure.Persistence.Parquet;

/// <summary>
/// Parquet.Net 6 の ParquetSerializer を活用した、堅牢でモダンなストレージサービス実装。
/// </summary>
public sealed class ParquetStorageService : BaseService, IParquetStorageService
{
    public async ValueTask WriteRecordsAsync<TRecord>(
        string filePath,
        IEnumerable<TRecord> records,
        CancellationToken cancellationToken = default) where TRecord : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(records);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // C# 13の await using による確実なリソース解放
        await using var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 65536,
            useAsync: true);

        // Parquet.Net 6の最新API：クラス構造をリフレクション/型生成でスキーマにマッピングし一括圧縮・保存
        await ParquetSerializer.SerializeAsync(records, fileStream, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<TRecord> ReadRecordsAsync<TRecord>(
            string filePath,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) where TRecord : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Parquet file not found at: {filePath}");
        }

        await using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 65536,
            useAsync: true);

        // 修正ポイント: 
        // 1. await で非同期的に全レコードのデシリアライズを完了させ、IList<TRecord> として取得する
        var records = await ParquetSerializer.DeserializeAsync<TRecord>(
            fileStream,
            cancellationToken: cancellationToken);

        // 2. 取得したリストを通常の foreach で回して yield return する
        foreach (var record in records.Data)
        {
            // キャンセル要求が来ている場合はここで中断
            cancellationToken.ThrowIfCancellationRequested();

            yield return record;
        }
    }
#if false
    public async IAsyncEnumerable<TRecord> ReadRecordsAsync<TRecord>(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where TRecord : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Parquet file not found at: {filePath}");
        }

        await using var fileStream = new FileStream(
            filePath, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize: 65536, 
            useAsync: true);

        // Parquet.Net 6の DeserializeAsync は IAsyncEnumerable を返すため、
        // 呼び出し元は foreach (await var record in ReadRecordsAsync(...)) でメモリを圧迫せずにストリーム処理可能。
        await foreach (var record in ParquetSerializer.DeserializeAsync<TRecord>(fileStream, cancellationToken: cancellationToken))
        {
            yield return record;
        }
    }
#endif
}
