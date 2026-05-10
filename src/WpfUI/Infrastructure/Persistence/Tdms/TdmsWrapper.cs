// ────────────────────────────────
//
// ────────────────────────────────

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WpfUI.Infrastructure.Persistence.Tdms.Native;

namespace WpfUI.Infrastructure.Persistence.Tdms;

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
        TdmsGuard.ThrowIfError(TdmNative.OpenFileEx(path, TdmNative.FILE_TYPE_TDM_STREAMING, 1, out _handle));
    }
    public IReadOnlyList<TdmsPropertyInfo> GetPropertyInfos() =>
        GetPropertyInfosInternal(_handle!,
            TdmNative.GetNumFileProperties,
            TdmNative.GetFilePropertyNames,
            TdmNative.GetFilePropertyType);

    public string GetFileName()
        => GetStringProperty(_handle!, TdmNative.FILE_NAME, TdmNative.GetFileStringPropertyLength, TdmNative.GetFileProperty);

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
    public IReadOnlyList<TdmsPropertyInfo> GetPropertyInfos(GroupHandle group) =>
        GetPropertyInfosInternal(group,
            TdmNative.GetNumChannelGroupProperties,
            TdmNative.GetChannelGroupPropertyNames,
            TdmNative.GetChannelGroupPropertyType);

    public string GetChannelGroupName(GroupHandle group)
        => GetStringProperty(group, TdmNative.CHANNELGROUP_NAME, TdmNative.GetChannelGroupStringPropertyLength, TdmNative.GetChannelGroupProperty);

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

    public IReadOnlyList<TdmsPropertyInfo> GetPropertyInfos(ChannelHandle channel) =>
        GetPropertyInfosInternal(channel,
            TdmNative.GetNumChannelProperties,
            TdmNative.GetChannelPropertyNames,
            TdmNative.GetChannelPropertyType);

    public string GetChannelName(ChannelHandle channel)
        => GetStringProperty(channel, TdmNative.CHANNEL_NAME, TdmNative.GetChannelStringPropertyLength, TdmNative.GetChannelProperty);
    public string GetChannelUnit(ChannelHandle channel)
        => GetStringProperty(channel, TdmNative.CHANNEL_UNIT_STRING, TdmNative.GetChannelStringPropertyLength, TdmNative.GetChannelProperty);
    public string GetChannelDescription(ChannelHandle channel)
        => GetStringProperty(channel, TdmNative.CHANNEL_DESCRIPTION, TdmNative.GetChannelStringPropertyLength, TdmNative.GetChannelProperty);

    public ChannelHandle GetChannelByName(GroupHandle group, string name)
    => FindByNameInternal(GetChannels(group), name, GetChannelName);

    public ChannelHandle GetChannelByName(string nameGroup, string nameChannel)
    => GetChannelByName(GetChannelGroupByName(nameGroup), nameChannel);

    public ulong GetNumDataValues(ChannelHandle channel)
    {
        TdmsGuard.ThrowIfError(TdmNative.GetNumDataValues(channel, out var numValues));
        return numValues;
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



    #region Private Helpers

    /// <summary>
    /// Generic helper to retrieve string properties from native handles with optimized memory usage.
    /// </summary>
    private delegate TdmNative.ErrorCode GetLengthDelegate<T>(T handle, string property, out uint length);
    private delegate TdmNative.ErrorCode GetValueDelegate<T>(T handle, string property, Span<byte> buffer, nuint length);

    private static string GetStringProperty<THandle>(
        THandle handle,
        string propertyName,
        GetLengthDelegate<THandle> lengthFunc,
        GetValueDelegate<THandle> valueFunc) where THandle : TdmHandle
    {
        if (handle.IsInvalid) { return string.Empty; }
        TdmsGuard.ThrowIfError(lengthFunc(handle, propertyName, out var length));
        if (length == 0) { return string.Empty; }

        // Use ArrayPool to avoid heavy allocations for large metadata strings
        byte[]? rented = null;
        Span<byte> buffer = length <= StackAllocThreshold
            ? stackalloc byte[(int)length]
            : (rented = ArrayPool<byte>.Shared.Rent((int)length));

        try
        {
            TdmsGuard.ThrowIfError(valueFunc(handle, propertyName, buffer, length));

            // Trim null terminators and convert to string
            var actualData = buffer[..(int)length].TrimEnd((byte)0);
            return Encoding.UTF8.GetString(actualData);
        }
        finally
        {
            if (rented != null) { ArrayPool<byte>.Shared.Return(rented); }
        }
    }

    /// <summary>
    /// Group / Channel
    /// </summary>
    private delegate TdmNative.ErrorCode GetCountInvoker<THandle>(THandle handle, out uint count);
    private delegate TdmNative.ErrorCode GetNamesInvoker<THandle>(THandle handle, Span<nint> names, nuint count);
    private delegate TdmNative.ErrorCode GetTypeInvoker<THandle>(THandle handle, string name, out TdmNative.DataType type);

    private static IReadOnlyList<TdmsPropertyInfo> GetPropertyInfosInternal<THandle>(
        THandle handle,
        GetCountInvoker<THandle> getCount,
        GetNamesInvoker<THandle> getNames,
        GetTypeInvoker<THandle> getType) where THandle : TdmHandle
    {
        if (handle.IsInvalid) return [];
        TdmsGuard.ThrowIfError(getCount(handle, out var count));
        if (count == 0) return [];

        nint[]? rented = null;
        Span<nint> ptrSpan = count <= 128
            ? stackalloc nint[(int)count]
            : (rented = ArrayPool<nint>.Shared.Rent((int)count)).AsSpan(0, (int)count);
        try
        {
            nint[] buffer = rented ?? ptrSpan.ToArray();
            TdmsGuard.ThrowIfError(getNames(handle, buffer, (nuint)count));

            var result = new List<TdmsPropertyInfo>((int)count);
            for (int i = 0; i < (int)count; i++)
            {
                nint ptr = buffer[i];
                if (ptr == nint.Zero) continue;

                string? name = Marshal.PtrToStringUTF8(ptr);
                if (string.IsNullOrEmpty(name)) continue;

                TdmsGuard.ThrowIfError(getType(handle, name, out var dataType));
                result.Add(new TdmsPropertyInfo(name, dataType));
            }
            return result;
        }
        finally
        {
            foreach (var ptr in ptrSpan) { if (ptr != nint.Zero) TdmNative.FreeMemory(ptr); }
            if (rented is not null) { ArrayPool<nint>.Shared.Return(rented); }
        }
    }

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
    #endregion
}
