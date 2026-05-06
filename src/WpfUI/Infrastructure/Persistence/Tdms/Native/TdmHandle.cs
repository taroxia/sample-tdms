namespace WpfUI.Infrastructure.Persistence.Tdms.Native;

using Microsoft.Win32.SafeHandles;

internal abstract class TdmHandle(Func<nint, TdmNative.ErrorCode> releaseFunc)
    : SafeHandleZeroOrMinusOneIsInvalid(true)
{
    /// <summary>
    /// Sets the native handle. Throws if the handle is already initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to overwrite an existing valid handle.</exception>
    public TdmHandle Initialize(nint h)
    {
        if (!IsInvalid)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is already initialized with handle {handle}. Cannot overwrite.");
        }
        SetHandle(h);
        return this;
    }

    public static implicit operator nint(TdmHandle? handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;

    protected override bool ReleaseHandle() => releaseFunc(handle) == 0;
}

internal sealed class FileHandle()
    : TdmHandle(TdmNative.CloseFile)
{
    public static implicit operator FileHandle(nint h) => (FileHandle)new FileHandle().Initialize(h);
}

internal sealed class GroupHandle()
    : TdmHandle(TdmNative.CloseChannelGroup)
{
    public static implicit operator GroupHandle(nint h) => (GroupHandle)new GroupHandle().Initialize(h);
}

internal sealed class ChannelHandle()
    : TdmHandle(TdmNative.CloseChannel)
{
    public static implicit operator ChannelHandle(nint h) => (ChannelHandle)new ChannelHandle().Initialize(h);
}
