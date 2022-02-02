using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using JAudio.Utils;

namespace JAudio.SoundData
{
    /// <summary>
    /// Bank containing information about each sample.
    /// </summary>
    public class SampleBank
    {
        /// <summary>
        /// Initialize a new sample bank from the specified stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        public SampleBank(Stream stream)
        {
            try
            {
                // Open the stream for binary reading
                reader = new BinaryReader(stream);

                // Check whether the stream is a valid sound data file and throw an exception if not
                if (Endianness.Swap(reader.ReadUInt32()) != MagicNumber) throw new FileFormatException("Unrecognized file type. The file might be corrupted truncated or in an unexpected format.");

                reader.BaseStream.Position += 0xc;
                WinfChunkOffset = Endianness.Swap(reader.ReadUInt32());
                WbctChunkOffset = Endianness.Swap(reader.ReadUInt32());

                reader.BaseStream.Position = WinfChunkOffset;
                if (Endianness.Swap(reader.ReadUInt32()) != (uint)ChunkIdentifiers.Winf) throw FormatExc;
                int files = Endianness.Swap(reader.ReadInt32());
                SoundFiles = new List<string>(files);

                for (int i = 0; i < files; i++)
                {
                    reader.BaseStream.Position = WinfChunkOffset + 8 + 4 * i;
                    reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32());

                    List<byte> byteList = new List<byte>();
                    while (true)
                    {
                        byte b = reader.ReadByte();
                        if (b == 0x0) break;
                        else byteList.Add(b);
                    }
                    SoundFiles.Add(Encoding.ASCII.GetString(byteList.ToArray()));
                }

                Samples = new Dictionary<short, SampleLocation>();

                reader.BaseStream.Position = WbctChunkOffset;
                if (Endianness.Swap(reader.ReadUInt32()) != (uint)ChunkIdentifiers.Wbct) throw FormatExc;
                reader.BaseStream.Position += 4;
                if (Endianness.Swap(reader.ReadUInt32()) != SoundFiles.Count) throw FormatExc;

                for (uint u = 0; u < (uint)SoundFiles.Count; u++)
                {
                    reader.BaseStream.Position = WbctChunkOffset + 0xc + 4 * u;
                    reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32());
                    if (Endianness.Swap(reader.ReadUInt32()) != (uint)ChunkIdentifiers.Scne) throw FormatExc;

                    reader.BaseStream.Position += 8;
                    reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32());
                    if (Endianness.Swap(reader.ReadUInt32()) != (uint)ChunkIdentifiers.CDf) throw FormatExc;
                    uint samples = Endianness.Swap(reader.ReadUInt32());
                    uint pos = (uint)reader.BaseStream.Position;

                    for (int u2 = 0; u2 < samples; u2++)
                    {
                        reader.BaseStream.Position = pos + 4 * u2;
                        reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32());
                        int wsys = Endianness.Swap(reader.ReadInt16());
                        short sample = Endianness.Swap(reader.ReadInt16());

                        if (!Samples.ContainsKey(sample)) Samples.Add(sample, new SampleLocation() { Index = u2, Wsys = wsys });
                    }
                }
            }

            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Get the sample with the specified ID.
        /// </summary>
        /// <param name="sample">The sample ID.</param>
        /// <returns>Sample.</returns>
        public Sample this[short sample]
        {
            get
            {
                if (!Samples.ContainsKey(sample)) throw new SampleNotFoundException(string.Format("Sample {0:x4} not found.", sample));

                reader.BaseStream.Position = WinfChunkOffset + 8 + 4 * Samples[sample].Wsys;
                reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32()) + 0x74 + 4 * Samples[sample].Index;
                reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32()) + 2;

                byte rootKey = reader.ReadByte();
                reader.BaseStream.Position += 2;
                int sampleRate = Endianness.Swap(reader.ReadInt16());
                reader.BaseStream.Position += 1;
                int entryOffset = Endianness.Swap(reader.ReadInt32());
                int entrySize = Endianness.Swap(reader.ReadInt32());
                bool isLooping = (reader.ReadUInt32() == 0xffffffff) ? true : false;
                int loopStart = Endianness.Swap(reader.ReadInt32());
                int loopEnd = Endianness.Swap(reader.ReadInt32());

                // TEMPORARY SOLUTION!
                // The extracted wave files are read.
                string filename = string.Format(Z2Sound.SoundPath + "\\Waves\\{0}_{1:x8}.wav",
                    SoundFiles[(int)Samples[sample].Wsys], Samples[sample].Index);
                WaveFile wave = new WaveFile(File.OpenRead(filename));



                byte[] data;
                if (isLooping)
                {
                    // The rest after the loop end position is cut off.
                    int size = (int)(loopEnd * (wave.Format.BitsPerSample / 8) * wave.Format.Channels);
                    data = new byte[size];
                    Array.Copy(wave.GetAudioData(), data, size);
                }
                else
                {
                    data = wave.GetAudioData();
                }

                return new Sample() { BitsPerSample = 16, Channels = 1, IsLooping = isLooping, LoopStart = loopStart, RootKey = rootKey,
                    SamplesPerSecond = (int)wave.Format.SamplesPerSecond, Data = data };
            }
        }

        /// <summary>
        /// Sample table.
        /// </summary>
        private Dictionary<short, SampleLocation> Samples;

        /// <summary>
        /// List containing the filename of each sound archive.
        /// </summary>
        private List<string> SoundFiles;

        /// <summary>
        /// Offset off the WINF chunk.
        /// </summary>
        private uint WinfChunkOffset { get; set; }

        /// <summary>
        /// Offset of the WBCT chunk.
        /// </summary>
        private uint WbctChunkOffset { get; set; }


        private BinaryReader reader;

        /// <summary>
        /// Structure holding the location of a sample.
        /// </summary>
        private struct SampleLocation
        {
            private int _Wsys;
            private int _Index;
            private int _Offset;

            public int Wsys
            {
                get { return _Wsys; }
                set { _Wsys = value; }
            }

            public int Index
            {
                get { return _Index;}
                set { _Index = value; }
            }

            public int Offset
            {
                get { return _Offset; }
                set { _Offset = value; }
            }
        }

        static FileFormatException FormatExc = new FileFormatException("The file might be corrupted truncated or in an unexpected format.");

        #region FourCCs

        /// <summary>
        /// Represents the FourCC magic number of a sample bank.
        /// </summary>
        private const uint MagicNumber = 0x57535953;

        /// <summary>
        /// Chunk identifiers.
        /// </summary>
        private enum ChunkIdentifiers : uint
        {
            Wbct = 0x57424354,
            Winf = 0x57494E46,
            CDf = 0x432D4446,
            CEx = 0x432D4558,
            CSt = 0x432D5354,
            Scne = 0x53434E45
        }

        #endregion
    }
}
