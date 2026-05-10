// ────────────────────────────────
//
// ────────────────────────────────

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WpfUI.Infrastructure.Persistence.Tdms.Native;

/// <summary>
/// P/Invoke 定義クラス（nilibddc.dll / nilibddc.h 対応・関数漏れなし）
/// </summary>
internal static partial class TdmNative
{
    private const string LibName = "nilibddc.dll";

    public enum DataType
    {
        UInt8 = 5,
        Int16 = 2,
        Int32 = 3,
        Float = 9,
        Double = 10,
        String = 23,
        Timestamp = 30,
    }

    public enum ErrorCode : int
    {
        NoError = 0,
        ErrorBegin = -6201,

        OutOfMemory = -6201,
        InvalidArgument = -6202,
        InvalidDataType = -6203,
        UnexpectedError = -6204,
        UsiCouldNotBeLoaded = -6205,
        InvalidFileHandle = -6206,
        InvalidChannelGroupHandle = -6207,
        InvalidChannelHandle = -6208,
        FileDoesNotExist = -6209,
        CannotWriteToReadOnlyFile = -6210,
        StorageCouldNotBeOpened = -6211,
        FileAlreadyExists = -6212,
        PropertyDoesNotExist = -6213,
        PropertyDoesNotContainData = -6214,
        PropertyIsNotAScalar = -6215,
        DataObjectTypeNotFound = -6216,
        NotImplemented = -6217,
        CouldNotSaveFile = -6218,
        MaximumNumberOfDataValuesExceeded = -6219,
        InvalidChannelName = -6220,
        DuplicateChannelName = -6221,
        DataTypeNotSupported = -6222,
        FileAccessDenied = -6224,
        InvalidTimeValue = -6225,
        ReplaceNotSupportedForSavedTDMSData = -6226,
        PropertyDataTypeMismatch = -6227,
        ChannelDataTypeMismatch = -6228,

        ErrorEnd = -6228,
        ErrorForceSizeTo32Bits = unchecked((int)0xffffffff)
    }

    //======================================================================
    // 定数（#define）
    //======================================================================
    public const string FILE_TYPE_TDM = "TDM";
    public const string FILE_TYPE_TDM_STREAMING = "TDMS";

    public const string FILE_NAME = "name";
    public const string FILE_DESCRIPTION = "description";
    public const string FILE_TITLE = "title";
    public const string FILE_AUTHOR = "author";
    public const string FILE_DATETIME = "datetime";

    public const string CHANNELGROUP_NAME = "name";
    public const string CHANNELGROUP_DESCRIPTION = "description";

    public const string CHANNEL_NAME = "name";
    public const string CHANNEL_DESCRIPTION = "description";
    public const string CHANNEL_UNIT_STRING = "unit_string";
    public const string CHANNEL_MINIMUM = "minimum";
    public const string CHANNEL_MAXIMUM = "maximum";


    //*****************************************************************************
    /// -> Object Management
    //*****************************************************************************
    [LibraryImport(LibName, EntryPoint = "DDC_CreateFile", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode CreateFile(string filePath,
      string fileType,
      string name,
      string description,
      string title,
      string author,
      out FileHandle file);

    [LibraryImport(LibName, EntryPoint = "DDC_AddChannelGroup", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode AddChannelGroup(FileHandle file,
      string name,
      string description,
      out GroupHandle group);

    [LibraryImport(LibName, EntryPoint = "DDC_AddChannel", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode AddChannel(GroupHandle group,
       DataType dataType,
      string name,
      string description,
      string unitString,
      out ChannelHandle channel);

    [LibraryImport(LibName, EntryPoint = "DDC_SaveFile")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SaveFile(FileHandle file);

    [LibraryImport(LibName, EntryPoint = "DDC_CloseFile")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CloseFile(nint file);

    [LibraryImport(LibName, EntryPoint = "DDC_OpenFileEx", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode OpenFileEx(string filePath,
      string fileType,
      int readOnly,
      out FileHandle file);

    //*****************************************************************************
    /// -> Advanced
    //*****************************************************************************
    [LibraryImport(LibName, EntryPoint = "DDC_RemoveChannelGroup")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode RemoveChannelGroup(GroupHandle group);

    [LibraryImport(LibName, EntryPoint = "DDC_RemoveChannel")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode RemoveChannel(ChannelHandle channel);

    [LibraryImport(LibName, EntryPoint = "DDC_CloseChannelGroup")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CloseChannelGroup(nint group);

    [LibraryImport(LibName, EntryPoint = "DDC_CloseChannel")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CloseChannel(nint channel);

    //*****************************************************************************
    /// <- Advanced
    //*****************************************************************************
    //*****************************************************************************
    /// -> Obsolete
    //*****************************************************************************
    [LibraryImport(LibName, EntryPoint = "DDC_OpenFile", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode OpenFile(string filePath,
      string fileType,
      out FileHandle file);

    //*****************************************************************************
    /// <- Obsolete
    //*****************************************************************************
    //*****************************************************************************
    /// <- Object Management
    //*****************************************************************************

    //*****************************************************************************
    /// -> Data Storage
    //*****************************************************************************
    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValues")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValues(ChannelHandle channel,
       IntPtr values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValuesTimestampComponents")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValuesTimestampComponents(ChannelHandle channel,
         uint[] year,
         uint[] month,
         uint[] day,
         uint[] hour,
         uint[] minute,
         uint[] second,
         double[] milliSecond,
         nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValues")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValues(ChannelHandle channel,
       IntPtr values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValuesTimestampComponents")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValuesTimestampComponents(ChannelHandle channel,
 uint[] year,
         uint[] month,
         uint[] day,
         uint[] hour,
         uint[] minute,
         uint[] second,
         double[] milliSecond,
         nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValues")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       IntPtr values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValuesTimestampComponents")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValuesTimestampComponents(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       uint[] year,
         uint[] month,
         uint[] day,
         uint[] hour,
         uint[] minute,
         uint[] second,
         double[] milliSecond,
         nuint numValues);

    //*****************************************************************************
    /// <- Data Storage
    //*****************************************************************************

    //*****************************************************************************
    /// -> Data Retrieval
    //*****************************************************************************

    //*****************************************************************************
    /// -> Enumeration
    //*****************************************************************************
    [LibraryImport(LibName, EntryPoint = "DDC_GetNumChannelGroups")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetNumChannelGroups(FileHandle file,
       out uint numChannelGroups);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroups")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroups(FileHandle file,
       Span<nint> channelGroupsBuf,
       nuint numChannelGroups);

    [LibraryImport(LibName, EntryPoint = "DDC_GetNumChannels")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetNumChannels(GroupHandle group,
       out uint numChannels);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannels")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannels(GroupHandle group,
       Span<nint> channelsBuf,
       nuint numChannels);

    //*****************************************************************************
    /// <- Enumeration
    //*****************************************************************************

    [LibraryImport(LibName, EntryPoint = "DDC_GetNumDataValues")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetNumDataValues(ChannelHandle channel,
       out ulong numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValues")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
       IntPtr values);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValuesTimestampComponents")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValuesTimestampComponents(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
          [Out] uint[] year,
         [Out] uint[] month,
         [Out] uint[] day,
         [Out] uint[] hour,
         [Out] uint[] minute,
         [Out] uint[] second,
         [Out] double[] milliSecond,
         [Out] uint[] weekDay);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataType")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataType(ChannelHandle channel,
       out DataType dataType);

    //*****************************************************************************
    /// <- Data Retrieval
    //*****************************************************************************

    //*****************************************************************************
    /// -> Properties
    //*****************************************************************************

    //*****************************************************************************
    /// -> File
    //*****************************************************************************
    // [LibraryImport(LibName, EntryPoint = "DDC_SetFileProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetFileProperty(FileHandle file, string property, int value);
    // [LibraryImport(LibName, EntryPoint = "DDC_SetFileProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetFileProperty(FileHandle file, string property, double value);
    // [LibraryImport(LibName, EntryPoint = "DDC_SetFileProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetFileProperty(FileHandle file, string property, string value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFilePropertyTimestampComponents(FileHandle file,
       string property,
       uint year,
       uint month,
       uint day,
       uint hour,
       uint minute,
       uint second,
       double milliSecond);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyV", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFilePropertyV(FileHandle file,
       string property,
       IntPtr va_list_args);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFileProperty", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileProperty(FileHandle file,
       string property,
       IntPtr value,
       nuint valueSizeInBytes);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFilePropertyTimestampComponents(FileHandle file,
       string property,
       out uint year,
       out uint month,
       out uint day,
       out uint hour,
       out uint minute,
       out uint second,
       out double milliSecond,
       out uint weekDay);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFileStringPropertyLength", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileStringPropertyLength(FileHandle file,
       string property,
       out uint length);

    // [LibraryImport(LibName, EntryPoint = "DDC_CreateFileProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode CreateFileProperty(FileHandle file, string property,  DataType dataType, int value);
    // [LibraryImport(LibName, EntryPoint = "DDC_CreateFileProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode CreateFileProperty(FileHandle file, string property,  DataType dataType, double value);
    // [LibraryImport(LibName, EntryPoint = "DDC_CreateFileProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode CreateFileProperty(FileHandle file, string property,  DataType dataType, string value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFilePropertyTimestampComponents(FileHandle file,
       string property,
       uint year,
       uint month,
       uint day,
       uint hour,
       uint minute,
       uint second,
       double milliSecond);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyV", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFilePropertyV(FileHandle file,
       string property,
        DataType dataType,
       IntPtr va_list_args);

    [LibraryImport(LibName, EntryPoint = "DDC_FilePropertyExists", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode FilePropertyExists(FileHandle file,
       string property,
       out int exists);

    [LibraryImport(LibName, EntryPoint = "DDC_GetNumFileProperties")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetNumFileProperties(FileHandle file,
       out uint numProperties);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyNames", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFilePropertyNames(FileHandle file,
       Span<nint> propertyNames,
       nuint numPropertyNames);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyType", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFilePropertyType(FileHandle file,
       string property,
       out DataType dataType);

    //*****************************************************************************
    /// <- File
    //*****************************************************************************

    //*****************************************************************************
    /// -> Channel Group
    //*****************************************************************************
    // [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetChannelGroupProperty(GroupHandle group, string property, int value);
    // [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetChannelGroupProperty(GroupHandle group, string property, double value);
    // [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetChannelGroupProperty(GroupHandle group, string property, string value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupPropertyTimestampComponents(GroupHandle group,
       string property,
       uint year,
       uint month,
       uint day,
       uint hour,
       uint minute,
       uint second,
       double milliSecond);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyV", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupPropertyV(GroupHandle group,
       string property,
       IntPtr va_list_args);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupProperty", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupProperty(GroupHandle group,
          string property,
          IntPtr value,
          nuint valueSizeInBytes);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupPropertyTimestampComponents(GroupHandle group,
       string property,
       out uint year,
       out uint month,
       out uint day,
       out uint hour,
       out uint minute,
       out uint second,
       out double milliSecond,
       out uint weekDay);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupStringPropertyLength", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupStringPropertyLength(GroupHandle group,
       string property,
       out uint length);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupProperty", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group, string property, DataType dataType, int value);
    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupProperty", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group, string property, DataType dataType, double value);
    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupProperty", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group, string property, DataType dataType, string value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupPropertyTimestampComponents(GroupHandle group,
       string property,
       uint year,
       uint month,
       uint day,
       uint hour,
       uint minute,
       uint second,
       double milliSecond);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyV", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupPropertyV(GroupHandle group,
        string property,
         DataType dataType,
       IntPtr va_list_args);

    [LibraryImport(LibName, EntryPoint = "DDC_ChannelGroupPropertyExists", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ChannelGroupPropertyExists(GroupHandle group,
          string property,
          out int exists);

    [LibraryImport(LibName, EntryPoint = "DDC_GetNumChannelGroupProperties")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetNumChannelGroupProperties(GroupHandle group,
       out uint numProperties);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyNames")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupPropertyNames(GroupHandle group,
          Span<nint> propertyNames,
          nuint numPropertyNames);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyType", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupPropertyType(GroupHandle group,
          string property,
          out DataType dataType);

    //*****************************************************************************
    /// <- Channel Group
    //*****************************************************************************

    //*****************************************************************************
    /// -> Channel
    //*****************************************************************************
    // [LibraryImport(LibName, EntryPoint = "DDC_SetChannelProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetChannelProperty(ChannelHandle channel, string property, int value);
    // [LibraryImport(LibName, EntryPoint = "DDC_SetChannelProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetChannelProperty(ChannelHandle channel, string property, double value);
    // [LibraryImport(LibName, EntryPoint = "DDC_SetChannelProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode SetChannelProperty(ChannelHandle channel, string property, string value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelPropertyTimestampComponents(ChannelHandle channel,
          string property,
          uint year,
          uint month,
          uint day,
          uint hour,
          uint minute,
          uint second,
          double milliSecond);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyV", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelPropertyV(ChannelHandle channel,
           string property,
           IntPtr va_list_args);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelProperty", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelProperty(ChannelHandle channel,
            string property,
            IntPtr value,
            nuint valueSizeInBytes);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelPropertyTimestampComponents(ChannelHandle channel,
       string property,
       out uint year,
       out uint month,
       out uint day,
       out uint hour,
       out uint minute,
       out uint second,
       out double milliSecond,
       out uint weekDay);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelStringPropertyLength", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelStringPropertyLength(ChannelHandle channel,
       string property,
       out uint length);

    // [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] // __cdecl 
    // public static partial ErrorCode CreateChannelProperty(ChannelHandle channel, string property,  DataType dataType, int value);
    // [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode CreateChannelProperty(ChannelHandle channel, string property,  DataType dataType, double value);
    // [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelProperty", StringMarshalling = StringMarshalling.Utf8)]
    // [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    // public static partial ErrorCode CreateChannelProperty(ChannelHandle channel, string property,  DataType dataType, string value);


    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyTimestampComponents", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelPropertyTimestampComponents(ChannelHandle channel,
       string property,
       uint year,
       uint month,
       uint day,
       uint hour,
       uint minute,
       uint second,
       double milliSecond);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyV", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelPropertyV(ChannelHandle channel,
          string property,
           DataType dataType,
          IntPtr va_list_args);

    [LibraryImport(LibName, EntryPoint = "DDC_ChannelPropertyExists", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ChannelPropertyExists(ChannelHandle channel,
       string property,
       out int exists);

    [LibraryImport(LibName, EntryPoint = "DDC_GetNumChannelProperties")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetNumChannelProperties(ChannelHandle channel,
       out uint numProperties);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyNames")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelPropertyNames(ChannelHandle channel,
       Span<nint> propertyNames,
       nuint numPropertyNames);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyType", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelPropertyType(ChannelHandle channel,
      string property,
      out DataType dataType);


    //*****************************************************************************
    /// <- Channel
    //*****************************************************************************

    //*****************************************************************************
    /// <- Properties
    //*****************************************************************************

    //*****************************************************************************
    /// -> Miscellaneous
    //*****************************************************************************
    [LibraryImport(LibName, EntryPoint = "DDC_GetLibraryErrorDescription", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial IntPtr GetLibraryErrorDescription(ErrorCode errorCode);

    [LibraryImport(LibName, EntryPoint = "DDC_FreeMemory")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial void FreeMemory(IntPtr memoryPointer);

    //*****************************************************************************
    /// <- Miscellaneous
    //*****************************************************************************

    //*****************************************************************************
    /// -> Separate type-safe functions for non-C users
    //*****************************************************************************
    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValuesUInt8")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValues(ChannelHandle channel,
       byte[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValuesInt16")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValues(ChannelHandle channel,
       short[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValuesInt32")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValues(ChannelHandle channel,
       int[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValuesFloat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValues(ChannelHandle channel,
       float[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValuesDouble")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValues(ChannelHandle channel,
       double[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_SetDataValuesString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetDataValues(ChannelHandle channel,
       string[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValuesUInt8")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValues(ChannelHandle channel,
       byte[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValuesInt16")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValues(ChannelHandle channel,
       short[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValuesInt32")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValues(ChannelHandle channel,
       int[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValuesFloat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValues(ChannelHandle channel,
       float[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValuesDouble")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValues(ChannelHandle channel,
       double[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_AppendDataValuesString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode AppendDataValues(ChannelHandle channel,
       string[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValuesUInt8")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       byte[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValuesInt16")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       short[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValuesInt32")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       int[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValuesFloat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       float[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValuesDouble")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       double[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_ReplaceDataValuesString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode ReplaceDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToReplace,
       string[] values,
       nuint numValues);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValuesUInt8")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
       [Out] byte[] values);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValuesInt16")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
       [Out] short[] values);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValuesInt32")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
       [Out] int[] values);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValuesFloat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
       [Out] float[] values);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValuesDouble")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
       Span<double> values);

    [LibraryImport(LibName, EntryPoint = "DDC_GetDataValuesString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetDataValues(ChannelHandle channel,
       nuint indexOfFirstValueToGet,
       nuint numValuesToGet,
       [Out] string[] values);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFileProperty(FileHandle file,
       string property,
       byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFileProperty(FileHandle file,
       string property,
       short value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFileProperty(FileHandle file,
       string property,
       int value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFileProperty(FileHandle file,
       string property,
       float value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFileProperty(FileHandle file,
       string property,
       double value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateFilePropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateFileProperty(FileHandle file,
       string property,
       string value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFileProperty(FileHandle file,
       string property,
       byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFileProperty(FileHandle file,
       string property,
       short value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFileProperty(FileHandle file,
       string property,
       int value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFileProperty(FileHandle file,
       string property,
       float value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFileProperty(FileHandle file,
       string property,
       double value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetFilePropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetFileProperty(FileHandle file,
       string property,
       string value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileProperty(FileHandle file,
       string property,
       out byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileProperty(FileHandle file,
       string property,
       out short value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileProperty(FileHandle file,
       string property,
       out int value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileProperty(FileHandle file,
       string property,
       out float value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileProperty(FileHandle file,
       string property,
       out double value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFileProperty(FileHandle file,
       string property,
       Span<byte> value,
       nuint valueSize);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group,
       string property,
       byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group,
       string property,
       short value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group,
       string property,
       int value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group,
       string property,
       float value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group,
       string property,
       double value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelGroupPropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelGroupProperty(GroupHandle group,
       string property,
       string value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupProperty(GroupHandle group,
       string property,
       byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupPrSetChannelGroupPropertyopertyInt16(GroupHandle group,
       string property,
       short value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupProperty(GroupHandle group,
       string property,
       int value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupProperty(GroupHandle group,
       string property,
       float value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupProperty(GroupHandle group,
       string property,
       double value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelGroupPropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelGroupProperty(GroupHandle group,
       string property,
       string value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupProperty(GroupHandle group,
       string property,
       out byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupProperty(GroupHandle group,
       string property,
       out short value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupProperty(GroupHandle group,
       string property,
       out int value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupProperty(GroupHandle group,
       string property,
       out float value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupProperty(GroupHandle group,
       string property,
       out double value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupProperty(GroupHandle group,
       string property,
       Span<byte> value,
       nuint valueSize);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelProperty(ChannelHandle channel,
       string property,
       byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelProperty(ChannelHandle channel,
       string property,
       short value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelProperty(ChannelHandle channel,
       string property,
       int value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelProperty(ChannelHandle channel,
       string property,
       float value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelProperty(ChannelHandle channel,
       string property,
       double value);

    [LibraryImport(LibName, EntryPoint = "DDC_CreateChannelPropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode CreateChannelProperty(ChannelHandle channel,
       string property,
       string value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelProperty(ChannelHandle channel,
       string property,
       byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelProperty(ChannelHandle channel,
       string property,
       short value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelProperty(ChannelHandle channel,
       string property,
       int value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelProperty(ChannelHandle channel,
       string property,
       float value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelProperty(ChannelHandle channel,
       string property,
       double value);

    [LibraryImport(LibName, EntryPoint = "DDC_SetChannelPropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode SetChannelProperty(ChannelHandle channel,
       string property,
       string value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyUInt8", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelProperty(ChannelHandle channel,
       string property,
       out byte value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyInt16", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelProperty(ChannelHandle channel,
       string property,
       out short value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyInt32", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelProperty(ChannelHandle channel,
       string property,
       out int value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyFloat", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelProperty(ChannelHandle channel,
       string property,
       out float value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyDouble", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelProperty(ChannelHandle channel,
       string property,
       out double value);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelProperty(ChannelHandle channel,
       string property,
       Span<byte> value,
       nuint valueSize);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyNameFromIndex")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFilePropertyNameFromIndex(FileHandle file,
       nuint index,
       Span<byte> propertyName,
       nuint propertyNameSize);

    [LibraryImport(LibName, EntryPoint = "DDC_GetFilePropertyNameLengthFromIndex")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetFilePropertyNameLengthFromIndex(FileHandle file,
       nuint index,
       out nuint propertyNameLength);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyNameFromIndex")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupPropertyNameFromIndex(GroupHandle group,
       nuint index,
       Span<byte> propertyName,
       nuint propertyNameSize);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelGroupPropertyNameLengthFromIndex")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelGroupPropertyNameLengthFromIndex(GroupHandle group,
       nuint index,
       out nuint propertyNameLength);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyNameFromIndex")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelPropertyNameFromIndex(ChannelHandle channel,
       nuint index,
       Span<byte> propertyName,
       nuint propertyNameSize);

    [LibraryImport(LibName, EntryPoint = "DDC_GetChannelPropertyNameLengthFromIndex")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static partial ErrorCode GetChannelPropertyNameLengthFromIndex(ChannelHandle channel,
       nuint index,
       out nuint propertyNameLength);

    //*****************************************************************************
    /// -> Separate type-safe functions for non-C users
    //*****************************************************************************
}
