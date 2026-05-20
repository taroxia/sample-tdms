// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfUI.Core.Collections;

/// <summary>
/// スレッドセーフで高性能な汎用LRU（Least Recently Used）キャッシュ
/// </summary>
public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly object _lock = new();
    private readonly LinkedList<TKey> _lruList = new();
    private readonly Dictionary<TKey, LinkedListNode<TKey>> _cacheMap = new();
    private readonly Dictionary<TKey, TValue> _valueMap = new();

    public LruCache(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "容量は1以上である必要があります。");
        _capacity = capacity;
    }

    /// <summary>
    /// キャッシュから値を取得します。存在しない場合は、ロックの外側で非同期ファクトリを実行して安全に生成・追加します。
    /// </summary>
    public async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueFactory);

        // 1. キャッシュヒット時の高速返却
        lock (_lock)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node); // 使用されたので先頭（最新）へ
                return _valueMap[key];
            }
        }

        // 2. キャッシュミス時はロックの外で非同期I/O・生成（Future-Based）を実行
        TValue computedValue = await valueFactory(key).ConfigureAwait(false);

        // 3. 安全にキャッシュへ追加
        lock (_lock)
        {
            // 待機中に他スレッドが同一キーで挿入済みの場合は、生成した方を破棄して既存の値を返す
            if (_cacheMap.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return _valueMap[key];
            }

            // キャッシュ上限超過時の古い要素のパージ
            if (_cacheMap.Count >= _capacity && _lruList.Last != null)
            {
                TKey oldestKey = _lruList.Last.Value;
                _lruList.RemoveLast();
                _cacheMap.Remove(oldestKey);
                _valueMap.Remove(oldestKey);
            }

            // 新規要素を先頭に追加
            var newNode = _lruList.AddFirst(key);
            _cacheMap[key] = newNode;
            _valueMap[key] = computedValue;
        }

        return computedValue;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _valueMap.Clear();
            _cacheMap.Clear();
            _lruList.Clear();
        }
    }
}
