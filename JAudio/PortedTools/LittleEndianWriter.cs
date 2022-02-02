using System;
using System.IO;
using System.Text;

namespace JAudio.Tools
{
    public class LittleEndianWriter : BinaryWriter
    {
        public LittleEndianWriter(FileStream fileStream) : base(fileStream) { }
        public LittleEndianWriter(MemoryStream memoryStream) : base(memoryStream) { }
        public LittleEndianWriter(Stream stream) : base(stream) { }
        public override void Write(string val)
        {
            base.Write(Encoding.ASCII.GetBytes(val));
        }
        public void Align()
        {
            while (BaseStream.Position % 4 != 0) Write((byte)0x00);
        }
        public void Align8()
        {
            while (BaseStream.Position % 8 != 0) Write((byte)0x00);
        }
        public void Align16()
        {
            while (BaseStream.Position % 16 != 0) Write((byte)0x00);
        }
        public void WriteNullTerminated(string text)
        {
            Write(text);
            Write((byte)0x00);
        }
        public void WriteCountString(string text)
        {
            if (text.Length > 0xFF)
                new Exception("String is longer than 255! Use the Int32 variant instead!");
            Write((byte)text.Length);
            Write(text);
        }
        public void WriteCountStringInt32(string text)
        {
            Write(text.Length);
            Write(text);
        }
        public long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }
    }
}
