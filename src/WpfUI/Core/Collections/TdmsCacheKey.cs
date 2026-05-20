// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfUI.Core.Collections;

public readonly struct TdmsCacheKey : IEquatable<TdmsCacheKey>
{
    public string FilePath { get; }
    public string GroupName { get; }
    public string ChannelName { get; }

    private readonly int _hashCode;

    public TdmsCacheKey(string filePath, string groupName, string channelName)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        ChannelName = channelName ?? throw new ArgumentNullException(nameof(channelName));

        _hashCode = HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(FilePath),
            StringComparer.Ordinal.GetHashCode(GroupName),
            StringComparer.Ordinal.GetHashCode(ChannelName)
        );
    }

    public bool Equals(TdmsCacheKey other)
    {
        if (_hashCode != other._hashCode) return false;

        return string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) &&
               string.Equals(GroupName, other.GroupName, StringComparison.Ordinal) &&
               string.Equals(ChannelName, other.ChannelName, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is TdmsCacheKey other && Equals(other);

    public override int GetHashCode() => _hashCode;

    public static bool operator ==(TdmsCacheKey left, TdmsCacheKey right) => left.Equals(right);
    public static bool operator !=(TdmsCacheKey left, TdmsCacheKey right) => !left.Equals(right);
}
