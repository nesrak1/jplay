using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JAudio.SoundData
{
    struct WaveFormat
    {
        public ushort Format { get; set; }
        public ushort Channels { get; set; }
        public uint SamplesPerSecond { get; set; }
        public ushort BitsPerSample { get; set; }
        public ushort BlockAlign { get { return (ushort)(Channels * (BitsPerSample / 0x8)); } }
        public uint BytesPerSecond { get { return BlockAlign * SamplesPerSecond; } }
    }
}
