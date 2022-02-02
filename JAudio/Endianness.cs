using System;

namespace JAudio.Utils
{
    /// <summary>
    /// Static class for converting big-endian values to little-endian values and vice versa.
    /// </summary>
    public static class Endianness
    {
        /// <summary>
        /// Returns the inverse byte order of a 2-byte signed integer.
        /// </summary>
        public static short Swap(short val)
        {
            return (short)(((val >> 8) & 0xff) + ((val << 8) & 0xff00));
        }

        /// <summary>
        /// Returns the inverse byte order of a 2-byte unsigned integer.
        /// </summary>
        public static ushort Swap(ushort val)
        {
            return (ushort)Swap((short)val);
        }

        /// <summary>
        /// Returns the inverse byte order of a 4-byte signed integer.
        /// </summary>
        public static int Swap(int val)
        {
            return ((val & 0xff) << 24) + ((val & 0xff00) << 8) + ((val & 0xff0000) >> 8) + ((val >> 24) & 0xff);
        }

        /// <summary>
        /// Returns the inverse byte order of a 4-byte unsigned integer.
        /// </summary>
        public static uint Swap(uint val)
        {
            return (uint)Swap((int)val);
        }

        /// <summary>
        /// Returns the inverse byte order of a 4-byte floating point value.
        /// </summary>
        public static float Swap(float val)
        {
            // Convert the floating point value to an Uint32 and use Endianness.Swap(Uint32) for the conversion
            return BitConverter.ToSingle(BitConverter.GetBytes(Endianness.Swap(BitConverter.ToUInt32(BitConverter.GetBytes(val), 0))), 0);
        }
    }
}
