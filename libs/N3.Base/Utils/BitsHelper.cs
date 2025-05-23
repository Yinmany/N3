namespace N3
{
    public static class BitsHelper
    {
        public static bool Has(ulong value, byte index)
        {
            return (value & (1UL << index)) != 0;
        }

        public static void Set(ref ulong value, byte index)
        {
            value |= 1UL << index;
        }

        public static void Unset(ref ulong value, byte index)
        {
            value &= ~(1UL << index);
        }
    }
}