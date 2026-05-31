// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfUI.Core.Dmain.Models;

public abstract record TdmsValue
{
    private TdmsValue() { }

    public record UInt8(byte Value) : TdmsValue;
    public record Int16(short Value) : TdmsValue;
    public record Int32(int Value) : TdmsValue;
    public record Float(float Value) : TdmsValue;
    public record Double(double Value) : TdmsValue;
    public record String(string Value) : TdmsValue;
    public record Timestamp(DateTime? Value) : TdmsValue;
    public sealed record Empty : TdmsValue { }
}


internal interface ICacheDisposable
{
    void InternalDispose();
}


public abstract record TdmsData : ICacheDisposable
{
    // コンストラクタは internal に変更（コンパイルエラーの回避）
    internal TdmsData() { }

    // 外部から不用意に呼ばせないため、明示的なインターフェース実装で隠蔽
    void ICacheDisposable.InternalDispose() => OnDispose();

    // 派生クラスのみがオーバーライドできる破棄ロジック
    protected abstract void OnDispose();

    // 各データ型定義（安全に IMemoryOwner を管理）
    public sealed record UInt8(IMemoryOwner<byte> Owner, int Length) : TdmsData
    {
        protected override void OnDispose() => Owner.Dispose();
    }
    public sealed record Int16(IMemoryOwner<short> Owner, int Length) : TdmsData
    {
        protected override void OnDispose() => Owner.Dispose();
    }
    public sealed record Int32(IMemoryOwner<int> Owner, int Length) : TdmsData
    {
        protected override void OnDispose() => Owner.Dispose();
    }
    public sealed record Float(IMemoryOwner<float> Owner, int Length) : TdmsData
    {
        protected override void OnDispose() => Owner.Dispose();
    }
    public sealed record Double(IMemoryOwner<double> Owner, int Length) : TdmsData
    {
        protected override void OnDispose() => Owner.Dispose();
    }
    public sealed record String(string[] Values) : TdmsData
    {
        protected override void OnDispose() { } // 何もしない 
    }
    public sealed record Timestamp(IMemoryOwner<DateTime> Owner, int Length) : TdmsData
    {
        protected override void OnDispose() => Owner.Dispose();
    }
    public sealed record Empty : TdmsData
    {
        protected override void OnDispose() { } // 何もしない 
    }
}
