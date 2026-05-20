// ────────────────────────────────
//
// ────────────────────────────────

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Windows.Documents;
using OpenTK.Audio.OpenAL;
using ScottPlot.TickGenerators.TimeUnits;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Infrastructure.Persistence.Tdms;

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

public abstract record TdmsData : IDisposable
{
    private TdmsData() { }

    public abstract void Dispose();

    public sealed record UInt8(IMemoryOwner<byte> Owner, int Length) : TdmsData { public override void Dispose() => Owner.Dispose(); }
    public sealed record Int16(IMemoryOwner<short> Owner, int Length) : TdmsData { public override void Dispose() => Owner.Dispose(); }
    public sealed record Int32(IMemoryOwner<int> Owner, int Length) : TdmsData { public override void Dispose() => Owner.Dispose(); }
    public sealed record Float(IMemoryOwner<float> Owner, int Length) : TdmsData { public override void Dispose() => Owner.Dispose(); }
    public sealed record Double(IMemoryOwner<double> Owner, int Length) : TdmsData { public override void Dispose() => Owner.Dispose(); }
    public sealed record String(string[] Values) : TdmsData { public override void Dispose() { } }  // no dispose.
    public sealed record Timestamp(IMemoryOwner<DateTime> Owner, int Length) : TdmsData { public override void Dispose() => Owner.Dispose(); }
    public sealed record Empty : TdmsData { public override void Dispose() { } }    // no dispose.
}

public sealed class TdmsException(int errorCode, string? message)
    : Exception($"TDM Error {errorCode}: {message}")
{
    public int ErrorCode { get; } = errorCode;
}

internal static class TdmsGuard
{
    /// <summary>
    /// Evaluates the native error code and throws TdmsException if not successful.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfError(TdmNative.ErrorCode errorCode, [CallerArgumentExpression(nameof(errorCode))] string? expression = null)
    {
        if (errorCode == TdmNative.ErrorCode.NoError) return;
        throw new TdmsException((int)errorCode, $"TDMS Native Error: {errorCode} in {expression}");
    }

    /// <summary>
    /// Ensures the file handle is initialized before any native operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureOpened([NotNull] FileHandle? handle)
    {
        if (handle is null || handle.IsInvalid)
            throw new InvalidOperationException("TDMS file is not opened or has been closed.");
    }
}

/// <summary>
/// High-performance wrapper for NI-TDM C DLL using .NET 9/C# 13 features.
/// Designed for large-scale data processing with minimal allocations.
/// </summary>
internal sealed class TdmsWrapper(string path) : IDisposable
{
    public readonly record struct TdmsPropertyInfo(string Name, TdmNative.DataType Type);

    private FileHandle? _handle;
    private bool _isDisposed;
    private const int StackAllocThreshold = 512;

    public void Dispose()
    {
        if (_isDisposed) return;
        _handle?.Dispose();
        _isDisposed = true;
    }

    /// <summary>
    /// Opens the TDMS file in streaming mode.
    /// </summary>
    public void OpenFile()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        TdmsGuard.ThrowIfError(TdmNative.OpenFileEx(path, TdmNative.FILE_TYPE_TDM_STREAMING, true, out _handle));
    }

    public string GetFileName() => GetStringProperty<FileHandle>(_handle!, TdmNative.FILE_NAME);

    public uint GetGroupCount()
    {
        TdmsGuard.EnsureOpened(_handle);
        TdmsGuard.ThrowIfError(TdmNative.GetNumChannelGroups(_handle, out var count));
        return count;
    }

    /// <summary>
    /// Retrieves all channel group handles in the current file.
    /// </summary>
    public GroupHandle[] GetChannelGroups()
    {
        TdmsGuard.EnsureOpened(_handle);
        var count = GetGroupCount();
        if (count == 0) return [];

        // Using GroupHandle (assuming it's a SafeHandle or pointer wrapper) directly
        Span<nint> rawHandles = stackalloc nint[(int)count];
        TdmsGuard.ThrowIfError(TdmNative.GetChannelGroups(_handle, rawHandles, count));
        return Array.ConvertAll(rawHandles.ToArray(), h => (GroupHandle)h);
    }

    public string GetChannelGroupName(GroupHandle group) => GetStringProperty(group, TdmNative.CHANNELGROUP_NAME);

    public GroupHandle GetChannelGroupByName(string name)
    => FindByNameInternal(GetChannelGroups(), name, GetChannelGroupName);

    public uint GetNumChannels(GroupHandle group)
    {
        TdmsGuard.ThrowIfError(TdmNative.GetNumChannels(group, out var count));
        return count;
    }

    /// <summary>
    /// Retrieves all channel handles within a specific group.
    /// </summary>
    public ChannelHandle[] GetChannels(GroupHandle group)
    {
        if (group.IsInvalid) return [];
        var count = GetNumChannels(group);
        if (count == 0) return [];

        Span<nint> rawHandles = stackalloc nint[(int)count];
        TdmsGuard.ThrowIfError(TdmNative.GetChannels(group, rawHandles, count));
        return Array.ConvertAll(rawHandles.ToArray(), h => (ChannelHandle)h);
    }

    public string GetChannelName(ChannelHandle channel) => GetStringProperty(channel, TdmNative.CHANNEL_NAME);
    public string GetChannelUnit(ChannelHandle channel) => GetStringProperty(channel, TdmNative.CHANNEL_UNIT_STRING);
    public string GetChannelDescription(ChannelHandle channel) => GetStringProperty(channel, TdmNative.CHANNEL_DESCRIPTION);

    public ChannelHandle GetChannelByName(GroupHandle group, string name)
    => FindByNameInternal(GetChannels(group), name, GetChannelName);

    public ChannelHandle GetChannelByName(string nameGroup, string nameChannel)
    => GetChannelByName(GetChannelGroupByName(nameGroup), nameChannel);

    // ==================================================
    // Properties.
    // ==================================================
    private static readonly IReadOnlyDictionary<string, TdmsValue> EmptyProperties =
        System.Collections.Immutable.ImmutableDictionary<string, TdmsValue>.Empty;
    public IReadOnlyDictionary<string, TdmsValue> GetProperties()
    {
        return GetProperties(_handle!);
    }

    public IReadOnlyDictionary<string, TdmsValue> GetProperties<THandle>(
        THandle handle) where THandle : TdmHandle
    {
        if (handle.IsInvalid) return EmptyProperties;

        uint count = 0;
        var errGetNum = handle switch
        {
            FileHandle f => TdmNative.GetNumProperties(f, out count),
            GroupHandle g => TdmNative.GetNumProperties(g, out count),
            ChannelHandle c => TdmNative.GetNumProperties(c, out count),
            _ => throw new NotSupportedException($"Unsupported handle type: {handle.GetType().Name}")
        };
        TdmsGuard.ThrowIfError(errGetNum);
        if (count == 0) return EmptyProperties;

        nint[]? rented = null;
        Span<nint> ptrSpan = count <= 128
            ? stackalloc nint[(int)count]
            : (rented = ArrayPool<nint>.Shared.Rent((int)count)).AsSpan(0, (int)count);
        try
        {
            nint[] buffer = rented ?? ptrSpan.ToArray();
            var errGetNames = handle switch
            {
                FileHandle f => TdmNative.GetPropertyNames(f, buffer, (nuint)count),
                GroupHandle g => TdmNative.GetPropertyNames(g, buffer, (nuint)count),
                ChannelHandle c => TdmNative.GetPropertyNames(c, buffer, (nuint)count),
                _ => throw new NotSupportedException($"Unsupported handle type:  {handle.GetType().Name}")
            };
            TdmsGuard.ThrowIfError(errGetNames);

            //var result = new List<TdmsPropertyInfo>((int)count);
            var result = new Dictionary<string, TdmsValue>();
            for (int i = 0; i < (int)count; i++)
            {
                nint ptr = buffer[i];
                if (ptr == nint.Zero) continue;

                string? name = Marshal.PtrToStringUTF8(ptr);
                if (string.IsNullOrEmpty(name)) continue;

                bool exists = false;
                var errExists = handle switch
                {
                    FileHandle f => TdmNative.PropertyExists(f, name, out exists),
                    GroupHandle g => TdmNative.PropertyExists(g, name, out exists),
                    ChannelHandle c => TdmNative.PropertyExists(c, name, out exists),
                    _ => throw new NotSupportedException($"Unsupported handle type: {handle.GetType().Name}")
                };
                TdmsGuard.ThrowIfError(errExists);
                if (!exists) continue;

                TdmNative.DataType dataType;
                var errGetType = handle switch
                {
                    FileHandle f => TdmNative.GetPropertyType(f, name, out dataType),
                    GroupHandle g => TdmNative.GetPropertyType(g, name, out dataType),
                    ChannelHandle c => TdmNative.GetPropertyType(c, name, out dataType),
                    _ => throw new NotSupportedException($"Unsupported handle type: {handle.GetType().Name}")
                };
                TdmsGuard.ThrowIfError(errGetType);

                TdmsValue value = (handle, dataType) switch
                {
                    (FileHandle f, TdmNative.DataType.UInt8) => new TdmsValue.UInt8(ReadValue<FileHandle, byte>(f, name, (FileHandle h, string p, out byte v) => TdmNative.GetProperty(h, p, out v))),
                    (FileHandle f, TdmNative.DataType.Int16) => new TdmsValue.Int16(ReadValue<FileHandle, short>(f, name, (FileHandle h, string p, out short v) => TdmNative.GetProperty(h, p, out v))),
                    (FileHandle f, TdmNative.DataType.Int32) => new TdmsValue.Int32(ReadValue<FileHandle, int>(f, name, (FileHandle h, string p, out int v) => TdmNative.GetProperty(h, p, out v))),
                    (FileHandle f, TdmNative.DataType.Float) => new TdmsValue.Float(ReadValue<FileHandle, float>(f, name, (FileHandle h, string p, out float v) => TdmNative.GetProperty(h, p, out v))),
                    (FileHandle f, TdmNative.DataType.Double) => new TdmsValue.Double(ReadValue<FileHandle, double>(f, name, (FileHandle h, string p, out double v) => TdmNative.GetProperty(h, p, out v))),
                    (FileHandle f, TdmNative.DataType.String) => new TdmsValue.String(GetStringProperty(f, name)),
                    (FileHandle f, TdmNative.DataType.Timestamp) => new TdmsValue.Timestamp(GetPropertyTimestampComponents(f, name)),

                    (GroupHandle g, TdmNative.DataType.UInt8) => new TdmsValue.UInt8(ReadValue<GroupHandle, byte>(g, name, (GroupHandle h, string p, out byte v) => TdmNative.GetProperty(h, p, out v))),
                    (GroupHandle g, TdmNative.DataType.Int16) => new TdmsValue.Int16(ReadValue<GroupHandle, short>(g, name, (GroupHandle h, string p, out short v) => TdmNative.GetProperty(h, p, out v))),
                    (GroupHandle g, TdmNative.DataType.Int32) => new TdmsValue.Int32(ReadValue<GroupHandle, int>(g, name, (GroupHandle h, string p, out int v) => TdmNative.GetProperty(h, p, out v))),
                    (GroupHandle g, TdmNative.DataType.Float) => new TdmsValue.Float(ReadValue<GroupHandle, float>(g, name, (GroupHandle h, string p, out float v) => TdmNative.GetProperty(h, p, out v))),
                    (GroupHandle g, TdmNative.DataType.Double) => new TdmsValue.Double(ReadValue<GroupHandle, double>(g, name, (GroupHandle h, string p, out double v) => TdmNative.GetProperty(h, p, out v))),
                    (GroupHandle g, TdmNative.DataType.String) => new TdmsValue.String(GetStringProperty(g, name)),
                    (GroupHandle g, TdmNative.DataType.Timestamp) => new TdmsValue.Timestamp(GetPropertyTimestampComponents(g, name)),

                    (ChannelHandle c, TdmNative.DataType.UInt8) => new TdmsValue.UInt8(ReadValue<ChannelHandle, byte>(c, name, (ChannelHandle h, string p, out byte v) => TdmNative.GetProperty(h, p, out v))),
                    (ChannelHandle c, TdmNative.DataType.Int16) => new TdmsValue.Int16(ReadValue<ChannelHandle, short>(c, name, (ChannelHandle h, string p, out short v) => TdmNative.GetProperty(h, p, out v))),
                    (ChannelHandle c, TdmNative.DataType.Int32) => new TdmsValue.Int32(ReadValue<ChannelHandle, int>(c, name, (ChannelHandle h, string p, out int v) => TdmNative.GetProperty(h, p, out v))),
                    (ChannelHandle c, TdmNative.DataType.Float) => new TdmsValue.Float(ReadValue<ChannelHandle, float>(c, name, (ChannelHandle h, string p, out float v) => TdmNative.GetProperty(h, p, out v))),
                    (ChannelHandle c, TdmNative.DataType.Double) => new TdmsValue.Double(ReadValue<ChannelHandle, double>(c, name, (ChannelHandle h, string p, out double v) => TdmNative.GetProperty(h, p, out v))),
                    (ChannelHandle c, TdmNative.DataType.String) => new TdmsValue.String(GetStringProperty(c, name)),
                    (ChannelHandle c, TdmNative.DataType.Timestamp) => new TdmsValue.Timestamp(GetPropertyTimestampComponents(c, name)),

                    _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                };
                result.Add(name, value);
            }
            return result;
        }
        finally
        {
            foreach (var ptr in ptrSpan) { if (ptr != nint.Zero) TdmNative.FreeMemory(ptr); }
            if (rented is not null) { ArrayPool<nint>.Shared.Return(rented); }
        }
    }

    // ==================================================
    // Data Values.
    // ==================================================
    public ulong GetNumDataValues(ChannelHandle channel)
    {
        TdmsGuard.ThrowIfError(TdmNative.GetNumDataValues(channel, out var numValues));
        return numValues;
    }

    public TdmNative.DataType GetDataType(ChannelHandle channel)
    {
        TdmsGuard.ThrowIfError(TdmNative.GetDataType(channel, out var dataType));
        return dataType;
    }

    public IMemoryOwner<double> GetDataValues(ChannelHandle channel, nuint offset, nuint count)
    {
        if (channel.IsInvalid) { return MemoryPool<double>.Shared.Rent(0); }

        // Use ArrayPool to rent a buffer. 
        // ArrayPool may return a buffer larger than requested for performance reasons.
        IMemoryOwner<double> owner = MemoryPool<double>.Shared.Rent((int)count);

        try
        {
            // Slice the memory to the exact requested size.
            Span<double> bufferSpan = owner.Memory.Span[..(int)count];
            TdmsGuard.ThrowIfError(TdmNative.GetDataValues(channel, offset, count, bufferSpan));
            return owner;
        }
        catch
        {
            // Safeguard for any unexpected errors within the method.
            owner.Dispose();
            throw;
        }
    }

    public TdmsData ReadChannel(ChannelHandle channel)
    {
        TdmsGuard.ThrowIfError(TdmNative.GetNumDataValues(channel, out var totalValues));
        if (totalValues == 0) return new TdmsData.Empty();

        int count = (int)totalValues;

        TdmsGuard.ThrowIfError(TdmNative.GetDataType(channel, out var dataType));

        return dataType switch
        {
            TdmNative.DataType.UInt8 => new TdmsData.UInt8(ReadPrimitive<byte>(channel, count, (ch, len, span) => TdmNative.GetDataValues(ch, 0, len, span)), count),
            TdmNative.DataType.Int16 => new TdmsData.Int16(ReadPrimitive<short>(channel, count, (ch, len, span) => TdmNative.GetDataValues(ch, 0, len, span)), count),
            TdmNative.DataType.Int32 => new TdmsData.Int32(ReadPrimitive<int>(channel, count, (ch, len, span) => TdmNative.GetDataValues(ch, 0, len, span)), count),
            TdmNative.DataType.Float => new TdmsData.Float(ReadPrimitive<float>(channel, count, (ch, len, span) => TdmNative.GetDataValues(ch, 0, len, span)), count),
            TdmNative.DataType.Double => new TdmsData.Double(ReadPrimitive<double>(channel, count, (ch, len, span) => TdmNative.GetDataValues(ch, 0, len, span)), count),
            TdmNative.DataType.String => ReadStrings(channel, count),
            TdmNative.DataType.Timestamp => new TdmsData.Timestamp(ReadTimestamps(channel, count), count),
            _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
        };
    }

    #region Private Helpers

    /// <summary>
    /// Generic helper to retrieve string properties from native handles with optimized memory usage.
    /// </summary>
    private static string GetStringProperty<THandle>(
        THandle handle,
        string property) where THandle : TdmHandle
    {
        if (handle.IsInvalid) { return string.Empty; }

        uint length = 0;
        var errorCode = handle switch
        {
            FileHandle f => TdmNative.GetStringPropertyLength(f, property, out length),
            GroupHandle g => TdmNative.GetStringPropertyLength(g, property, out length),
            ChannelHandle c => TdmNative.GetStringPropertyLength(c, property, out length),
            _ => throw new NotSupportedException($"Unsupported handle type: {handle.GetType().Name}")
        };
        if (errorCode == TdmNative.ErrorCode.PropertyDoesNotContainData) { return string.Empty; }
        TdmsGuard.ThrowIfError(errorCode);

        if (length == 0) { return string.Empty; }

        // Use ArrayPool to avoid heavy allocations for large metadata strings
        byte[]? rented = null;
        Span<byte> buffer = length <= StackAllocThreshold
            ? stackalloc byte[(int)length]
            : (rented = ArrayPool<byte>.Shared.Rent((int)length));

        try
        {
            var errForProp = handle switch
            {
                FileHandle f => TdmNative.GetProperty(f, property, buffer, length),
                GroupHandle g => TdmNative.GetProperty(g, property, buffer, length),
                ChannelHandle c => TdmNative.GetProperty(c, property, buffer, length),
                _ => throw new NotSupportedException($"Unsupported handle type: {handle.GetType().Name}")
            };
            TdmsGuard.ThrowIfError(errForProp);

            // Trim null terminators and convert to string
            var actualData = buffer[..(int)length].TrimEnd((byte)0);
            return Encoding.UTF8.GetString(actualData);
        }
        finally
        {
            if (rented != null) { ArrayPool<byte>.Shared.Return(rented); }
        }
    }

    private static DateTime? GetPropertyTimestampComponents<THandle>(
        THandle handle,
        string property) where THandle : TdmHandle
    {
        if (handle.IsInvalid) { return null; }

        uint year = 0;
        uint month = 0;
        uint day = 0;
        uint hour = 0;
        uint minute = 0;
        uint second = 0;
        double ms = 0;
        uint weekDay = 0;

        var errForTimestamp = handle switch
        {
            FileHandle f => TdmNative.GetPropertyTimestampComponents(f, property,
               out year, out month, out day,
               out hour, out minute, out second,
               out ms, out weekDay),

            GroupHandle g => TdmNative.GetPropertyTimestampComponents(g, property,
               out year, out month, out day,
               out hour, out minute, out second,
               out ms, out weekDay),
            ChannelHandle c => TdmNative.GetPropertyTimestampComponents(c, property,
               out year, out month, out day,
               out hour, out minute, out second,
               out ms, out weekDay),
            _ => throw new NotSupportedException($"Unsupported handle type: {handle.GetType().Name}")
        };

        if (errForTimestamp == TdmNative.ErrorCode.PropertyDoesNotContainData) { return null; }
        TdmsGuard.ThrowIfError(errForTimestamp);

        double totalMs = ms;
        int millisecond = (int)totalMs;
        int microsecond = (int)((totalMs - millisecond) * 1000);

        return new DateTime(
                (int)year, (int)month, (int)day,
                (int)hour, (int)minute, (int)second,
                millisecond, microsecond, DateTimeKind.Local);
    }

    /// <summary>
    /// Group / Channel
    /// </summary>

    /// <summary>
    /// Finds a specific handle by name from a collection and ensures all other handles are disposed.
    /// </summary>
    private THandle FindByNameInternal<THandle>(
        THandle[] handles,
        string name,
        Func<THandle, string> nameSelector) where THandle : TdmHandle, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (handles.Length == 0) return new THandle();

        ReadOnlySpan<char> target = name.AsSpan();
        THandle? match = null;

        try
        {
            foreach (var h in handles)
            {
                if (h.IsInvalid)
                {
                }
                else if (match is null && nameSelector(h).AsSpan().Equals(target, StringComparison.Ordinal))
                {
                    match = h;
                }
                else
                {
                    h.Dispose();
                }
            }
            return match ?? new THandle();
        }
        catch
        {
            foreach (var h in handles) h.Dispose();
            throw;
        }
    }

    private delegate TdmNative.ErrorCode GetPropertyFunc<THandle, TType>(THandle handle, string name, out TType value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TType ReadValue<THandle, TType>(THandle handle, string property,
    GetPropertyFunc<THandle, TType> getPropFunc) where TType : unmanaged
    {
        TType value = default;
        var err = getPropFunc(handle, property, out value);
        if (err == TdmNative.ErrorCode.PropertyDoesNotContainData) { return default; }
        TdmsGuard.ThrowIfError(err);
        return value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IMemoryOwner<T> ReadPrimitive<T>(
    ChannelHandle channel,
    int count,
    Func<ChannelHandle, nuint, Span<T>, TdmNative.ErrorCode> funGetDataValues) where T : unmanaged
    {
        IMemoryOwner<T> owner = MemoryPool<T>.Shared.Rent(count);
        Span<T> span = owner.Memory.Span[..count];

        TdmsGuard.ThrowIfError(funGetDataValues(channel, (nuint)count, span));
        return owner;
    }

    private static TdmsData.String ReadStrings(ChannelHandle channel, int count)
    {
        var array = new string[count];
        TdmsGuard.ThrowIfError(TdmNative.GetDataValues(channel, 0, (nuint)count, array));
        return new TdmsData.String(array);
    }

    private static IMemoryOwner<DateTime> ReadTimestamps(ChannelHandle channel, int count)
    {
        var poolUint = ArrayPool<uint>.Shared;
        var poolDouble = ArrayPool<double>.Shared;

        uint[] yearArr = poolUint.Rent(count);
        uint[] monthArr = poolUint.Rent(count);
        uint[] dayArr = poolUint.Rent(count);
        uint[] hourArr = poolUint.Rent(count);
        uint[] minuteArr = poolUint.Rent(count);
        uint[] secondArr = poolUint.Rent(count);
        double[] msArr = poolDouble.Rent(count);
        uint[] weekDayArr = poolUint.Rent(count);

        try
        {
            TdmsGuard.ThrowIfError(TdmNative.GetDataValuesTimestampComponents(
                channel, 0, (nuint)count,
                yearArr.AsSpan(0, count), monthArr.AsSpan(0, count), dayArr.AsSpan(0, count),
                hourArr.AsSpan(0, count), minuteArr.AsSpan(0, count), secondArr.AsSpan(0, count),
                msArr.AsSpan(0, count), weekDayArr.AsSpan(0, count)));

            IMemoryOwner<DateTime> dtOwner = MemoryPool<DateTime>.Shared.Rent(count);
            Span<DateTime> dtSpan = dtOwner.Memory.Span;

            for (int i = 0; i < count; i++)
            {
                double totalMs = msArr[i];
                int millisecond = (int)totalMs;
                int microsecond = (int)((totalMs - millisecond) * 1000);

                dtSpan[i] = new DateTime(
                    (int)yearArr[i], (int)monthArr[i], (int)dayArr[i],
                    (int)hourArr[i], (int)minuteArr[i], (int)secondArr[i],
                    millisecond, microsecond, DateTimeKind.Local);
            }
            return dtOwner;
        }
        finally
        {
            poolUint.Return(yearArr);
            poolUint.Return(monthArr);
            poolUint.Return(dayArr);
            poolUint.Return(hourArr);
            poolUint.Return(minuteArr);
            poolUint.Return(secondArr);
            poolDouble.Return(msArr);
            poolUint.Return(weekDayArr);
        }
    }
    #endregion
}
