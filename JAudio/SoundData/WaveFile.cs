using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JAudio.SoundData
{
    class WaveFile
    {
        public WaveFile() { }

        public WaveFile(Stream stream)
        {
            try
            {
                br = new BinaryReader(stream);

                if (br.ReadInt32() != 0x46464952) { throw new Exception("'RIFF' expected."); }
                br.BaseStream.Position += 4;
                if (br.ReadInt32() != 0x45564157) { throw new Exception("'WAVE' expected."); }

                uint pos = (uint)GetChunkPosition(0x20746D66);
                if (pos == 0) throw new Exception();
                br.BaseStream.Position = pos + 8;

                Format = new WaveFormat();
                Format.Format = (ushort)br.ReadInt16();
                Format.Channels = (ushort)br.ReadInt16();
                Format.SamplesPerSecond = (uint)br.ReadInt32();
                br.BaseStream.Position += 6;
                Format.BitsPerSample = (ushort)br.ReadInt16();

                if (Format.Format != 0x1) throw new Exception("Only PCM encoding is supported.");
            }

            catch (Exception e)
            {
                throw e;
            }
        }

        public byte[] GetAudioData()
        {
            uint pos = (uint)GetChunkPosition(0x61746164);

            if (pos == 0) throw new Exception("Could not find audio chunk.");
            else
            {
                br.BaseStream.Position = pos + 4;
                int size = br.ReadInt32();

                return br.ReadBytes(size);
            }
        }

        public void Save(string file, byte[] audioData)
        {
            bw = new BinaryWriter(File.OpenWrite(file));

            bw.Write(0x46464952);
            bw.Write(36 + audioData.Length);
            bw.Write(0x45564157);
            bw.Write(0x20746D66);
            bw.Write(16);
            bw.Write(Format.Format);
            bw.Write(Format.Channels);
            bw.Write(Format.SamplesPerSecond);
            bw.Write(Format.BytesPerSecond);
            bw.Write(Format.BlockAlign);
            bw.Write(Format.BitsPerSample);
            bw.Write(0x61746164);
            bw.Write(audioData.Length);
            bw.Write(audioData);
        }

        private int GetChunkPosition(uint chunkID)
        {
            int pos = 0xc;

            while (pos < br.BaseStream.Length - 4)
            {
                br.BaseStream.Position = pos;
                if (br.ReadInt32() == chunkID) return pos;
                else
                {
                    pos += br.ReadInt32() + 8;
                }
            }

            return 0;
        }

        public int AudioDataSize
        {
            get
            {
                uint pos = (uint)GetChunkPosition(0x61746164);
                if (pos == 0) return 0;
                else
                {
                    br.BaseStream.Position = pos + 4;
                    return br.ReadInt32();
                }
            }
        }

        public WaveFormat Format;

        BinaryReader br;
        BinaryWriter bw;
    }
}
