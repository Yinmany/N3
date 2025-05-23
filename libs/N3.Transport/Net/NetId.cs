using System.Runtime.InteropServices;

namespace N3.Network;

[StructLayout(LayoutKind.Explicit)]
internal readonly struct NetId
{
    [FieldOffset(0)] public readonly ushort Value;
    [FieldOffset(2)] public readonly ushort Version;
    [FieldOffset(0)] public readonly uint Id;

    public NetId(uint id)
    {
        this.Value = this.Version = 0;
        this.Id = id;
    }

    public NetId(ushort value, ushort version)
    {
        this.Id = 0;
        Value = value;
        Version = version;
    }
}