using System.IO;
using System.Text;

namespace JAudio.Tools
{
    public class BigEndianReader : BinaryReader
    {
        public BigEndianReader(Stream stream) : base(stream) { }
        public override short ReadInt16()
        {
            unchecked
            {
                return (short)ReverseShort((ushort)base.ReadInt16());
            }
        }
        public override ushort ReadUInt16()
        {
            unchecked
            {
                return ReverseShort(base.ReadUInt16());
            }
        }
        public override int ReadInt32()
        {
            unchecked
            {
                return (int)ReverseInt((uint)base.ReadInt32());
            }
        }
        public override uint ReadUInt32()
        {
            unchecked
            {
                return ReverseInt(base.ReadUInt32());
            }
        }
        public override long ReadInt64()
        {
            unchecked
            {
                return (long)ReverseLong((ulong)base.ReadInt64());
            }
        }
        public override ulong ReadUInt64()
        {
            unchecked
            {
                return ReverseLong(base.ReadUInt64());
            }
        }
        public ushort ReverseShort(ushort value)
        {
            return (ushort)(((value & 0xFF00) >> 8) | (value & 0x00FF) << 8);
        }
        public uint ReverseInt(uint value)
        {
            value = (value >> 16) | (value << 16);
            return ((value & 0xFF00FF00) >> 8) | ((value & 0x00FF00FF) << 8);
        }
        public ulong ReverseLong(ulong value)
        {
            value = (value >> 32) | (value << 32);
            value = ((value & 0xFFFF0000FFFF0000) >> 16) | ((value & 0x0000FFFF0000FFFF) << 16);
            return ((value & 0xFF00FF00FF00FF00) >> 8) | ((value & 0x00FF00FF00FF00FF) << 8);
        }
        public void Align()
        {
            long pad = 4 - (BaseStream.Position % 4);
            if (pad != 4) BaseStream.Position += pad;
        }
        public void Align8()
        {
            long pad = 8 - (BaseStream.Position % 8);
            if (pad != 8) BaseStream.Position += pad;
        }
        public void Align16()
        {
            long pad = 16 - (BaseStream.Position % 16);
            if (pad != 16) BaseStream.Position += pad;
        }
        public string ReadStringLength(int len)
        {
            return Encoding.ASCII.GetString(ReadBytes(len));
        }
        public string ReadNullTerminated()
        {
            string output = "";
            char curChar;
            while ((curChar = ReadChar()) != 0x00)
            {
                output += curChar;
            }
            return output;
        }
        public static string ReadNullTerminatedArray(byte[] bytes, uint pos)
        {
            string output = "";
            char curChar;
            while ((curChar = (char)bytes[pos]) != 0x00)
            {
                output += curChar;
                pos++;
            }
            return output;
        }
        public string ReadCountString()
        {
            byte length = ReadByte();
            return ReadStringLength(length);
        }
        public string ReadCountStringInt32()
        {
            int length = (int)ReadUInt32();
            return ReadStringLength(length);
        }
        public long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }
    }
}
