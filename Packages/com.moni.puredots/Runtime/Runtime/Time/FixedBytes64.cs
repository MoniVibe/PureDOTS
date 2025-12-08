using System.Runtime.InteropServices;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Fixed-size 64-byte storage suitable for Burst and deterministic logs.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = Size)]
    public unsafe struct FixedBytes64
    {
        public const int Size = 64;
        public fixed byte Buffer[Size];
    }
}
