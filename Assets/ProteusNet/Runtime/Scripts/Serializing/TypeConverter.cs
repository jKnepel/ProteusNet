using System.Runtime.InteropServices;

namespace jKnepel.ProteusNet.Serializing
{
    // memory layout conversion inspired by https://stackoverflow.com/a/619307
    public static class TypeConverter
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct UIntToFloat
		{
            [FieldOffset(0)]
            public uint UInt;
            [FieldOffset(0)]
            public float Float;
		}

        [StructLayout(LayoutKind.Explicit)]
        internal struct ULongToDouble
        {
            [FieldOffset(0)]
            public ulong ULong;
            [FieldOffset(0)]
            public double Double;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct ULongsToDecimal
        {
            [FieldOffset(0)]
            public ulong ULong1;
            [FieldOffset(8)]
            public ulong ULong2;
            [FieldOffset(0)]
            public decimal Decimal;
        }
    }
}
