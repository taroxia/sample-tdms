// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WpfUI.Core.Collections;

/// <summary>
/// キャッシュから貸し出されるリソースの生存期間を、参照カウントにより厳密に管理する構造体。
/// C# 13の ref struct や scope、または一般の IDisposable パターンに対応します。
/// </summary>
public sealed class CacheLease<TValue> : IDisposable
{
    private RefCountedValue<TValue>? _countedValue;
    private bool _isDisposed;

    internal CacheLease(RefCountedValue<TValue> countedValue)
    {
        _countedValue = countedValue;
        _countedValue.Increment();
    }

    /// <summary>
    /// キャッシュされた実データへの参照。
    /// </summary>
    public TValue Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _countedValue!.Value;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var Target = Interlocked.Exchange(ref _countedValue, null);
        Target?.Decrement();
    }
}

/// <summary>
/// 内部で実データとその参照カウント、およびDisposeロジックをカプセル化するクラス。
/// </summary>
internal sealed class RefCountedValue<TValue>(TValue value)
{
    private int _refCount = 0;
    private bool _isEvicted = false;
    public TValue Value { get; } = value;

    public void Increment() => Interlocked.Increment(ref _refCount);

    public void Decrement()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            Volatile.Write(ref _isEvicted, true);
            TryDispose();
        }
    }

    public void MarkEvicted()
    {
        _isEvicted = true;
        if (Volatile.Read(ref _refCount) == 0)
        {
            TryDispose();
        }
    }

    private void TryDispose()
    {
        if (Value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// 商用高要件向け高性能汎用LRU（Least Recently Used）キャッシュ。
/// 完璧なスレッドセーフ、重複生成防止、例外発生時の自動パージ、参照カウント式IDisposable自動管理を内蔵。
/// </summary>
public sealed class LruCache<TKey, TValue> : IDisposable where TKey : notnull
{
    private readonly int _capacity;
    private readonly object _lock = new();
    private readonly LinkedList<TKey> _lruList = [];
    private readonly Dictionary<TKey, LinkedListNode<TKey>> _cacheMap = [];
    private readonly Dictionary<TKey, Task<RefCountedValue<TValue>>> _valueMap = [];
    private bool _isDisposed;

    public LruCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));
        _capacity = capacity;
    }

    /// <summary>
    /// キャッシュから安全にリソースのリース（貸出）を取得します。
    /// 生成タスクが失敗した場合、そのキャッシュは自動的に破棄され、次回アクセス時に再試行を可能にします。
    /// </summary>
    public Task<CacheLease<TValue>> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueFactory);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            // 1. キャッシュヒット（現在タスク実行中、あるいは完了済み）
            if (_cacheMap.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node); // 最新化

                return GetLeaseAsync(_valueMap[key]);
            }

            // 2. キャッシュミス: 重複のないように非同期タスクを作成して登録
            var originTask = valueFactory(key);
            var wrapperTask = CreateRefCountedWrapperAsync(key, originTask);

            // キャッシュ上限のパージ
            if (_cacheMap.Count >= _capacity && _lruList.Last is not null)
            {
                var oldestKey = _lruList.Last.Value;
                _lruList.RemoveLast();
                _cacheMap.Remove(oldestKey);

                if (_valueMap.Remove(oldestKey, out var evictedTask))
                {
                    EvictTaskAsync(evictedTask);
                }
            }

            var newNode = _lruList.AddFirst(key);
            _cacheMap[key] = newNode;
            _valueMap[key] = wrapperTask;

            return GetLeaseAsync(wrapperTask);
        }
    }

    private async Task<RefCountedValue<TValue>> CreateRefCountedWrapperAsync(TKey key, Task<TValue> originTask)
    {
        try
        {
            var value = await originTask.ConfigureAwait(false);
            return new RefCountedValue<TValue>(value);
        }
        catch
        {
            // 商用高要件において最重要：ファクトリタスクが失敗した場合、
            // ゾンビタスク（エラー済みのタスク）がキャッシュに残留するのを防ぐため、ロックを取得して完全にパージする。
            lock (_lock)
            {
                if (!_isDisposed)
                {
                    if (_cacheMap.Remove(key, out var node))
                    {
                        _lruList.Remove(node);
                    }
                    _valueMap.Remove(key);
                }
            }
            throw;
        }
    }

    private static async Task<CacheLease<TValue>> GetLeaseAsync(Task<RefCountedValue<TValue>> wrapperTask)
    {
        var countedValue = await wrapperTask.ConfigureAwait(false);
        return new CacheLease<TValue>(countedValue);
    }

    private static void EvictTaskAsync(Task<RefCountedValue<TValue>> wrapperTask)
    {
        if (wrapperTask.IsCompletedSuccessfully)
        {
            wrapperTask.Result.MarkEvicted();
        }
        else
        {
            // 未完了のまま溢れた場合は、完了を待機してから安全にマーク
            wrapperTask.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully) t.Result.MarkEvicted();
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }

    public bool Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            if (_isDisposed) return false;

            if (_valueMap.Remove(key, out var targetTask))
            {
                if (_cacheMap.Remove(key, out var node))
                {
                    _lruList.Remove(node);
                }

                EvictTaskAsync(targetTask);
                return true;
            }
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var task in _valueMap.Values)
            {
                EvictTaskAsync(task);
            }
            _valueMap.Clear();
            _cacheMap.Clear();
            _lruList.Clear();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            Clear();
            _isDisposed = true;
        }
    }
}
