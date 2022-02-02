using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JAudio
{
    /// <summary>
    /// Class for reading virtual files from a stream.
    /// </summary>
    internal class VirtualStream : Stream
    {
        /// <summary>
        /// Create a new instance of the VirtualFileStream class.
        /// </summary>
        /// <param name="stream">The System.IO.Stream to load the virtual file from.</param>
        /// <param name="startPosition">The starting offset of the virtual file in the stream.</param>
        /// <param name="size">The size of the virtual file.</param>
        public VirtualStream(Stream stream, long startPosition, long size)
        {
            try
            {
                if (startPosition >= stream.Length) throw new ArgumentException("The starting offset cannot be greater than the size of the stream.", "startPosition");
                else if (startPosition + size > stream.Length) throw new ArgumentException("The virtual file cannot reach beyond the end of the stream.", "size");
                else
                {
                    if (size > 0) SetLength(size);
                    else SetLength(stream.Length - startPosition);

                    StartOffset = startPosition;
                    binaryReader = new BinaryReader(stream);
                    Position = 0x0;
                }
            }

            catch (Exception e)
            {
                throw e;
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get
            {
                return _Length;
            }
        }

        public override long Position
        {
            get
            {
                return _Position;
            }

            set
            {
                if (value < 0) throw new ArgumentException("The stream position cannot be less than zero.", "Position");
                else if (value > Length) throw new ArgumentException("The stream position must be less than the stream length.", "Position");
                else
                {
                    _Position = value;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                long origin = binaryReader.BaseStream.Position;
                binaryReader.BaseStream.Position = Position + StartOffset;
                int read = binaryReader.Read(buffer, offset, count);
                binaryReader.BaseStream.Position = origin;
                Position += read;
                return read;
            }

            catch (Exception e)
            {
                throw e;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;

                case SeekOrigin.Current:
                    Position += offset;
                    break;

                case SeekOrigin.End:
                    Position = Length - 1 - offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            _Length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        long StartOffset { get; set; }

        BinaryReader binaryReader;
        long _Position;
        long _Length;
    }
}
