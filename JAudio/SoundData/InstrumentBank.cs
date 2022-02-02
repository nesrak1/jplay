using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using JAudio.Utils;

namespace JAudio.SoundData
{
    /// <summary>
    /// Bank containing information about each instrument.
    /// </summary>
    public class InstrumentBank
    {
        /// <summary>
        /// Initialize a new instrument bank from the specified stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        public InstrumentBank(Stream stream, int wsys)
        {
            try
            {
                Wsys = wsys;

                // Open the stream for binary reading
                reader = new BinaryReader(stream);

                // Check whether the stream is a valid sound data file and throw an exception if not
                if (Endianness.Swap(reader.ReadUInt32()) != MagicNumber)
                    throw new FileFormatException("Unrecognized file type. The file might be corrupted, truncated or in an unexpected format.");

                // Search for the instrument list
                uint pos = GetChunkPosition((uint)ChunkIdentifiers.List);
                if (pos == 0) throw new FileFormatException("The instrument list could not be found. The file might be corrupted, truncated or in an unexpected format.");

                ListOffset = pos;
            }

            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Retrieves the instrument with the specified index.
        /// </summary>
        /// <param name="instrument">Number (index) of the instrument.</param>
        /// <returns>Instrument.</returns>
        public Instrument this[int instrument]
        {
            get
            {
                reader.BaseStream.Position = ListOffset;
                if (instrument >= Endianness.Swap(reader.ReadInt32())) throw new ArgumentOutOfRangeException("instrument",
                    "The instrument number must be less than the total amount of instruments.");
                reader.BaseStream.Position += 4 * instrument;
                reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32());
                uint chunkId = Endianness.Swap(reader.ReadUInt32());

                // The entry is a "normal" instrument
                if (chunkId == (uint)ChunkIdentifiers.InstEntry)
                {
                    Instrument inst = new Instrument();

                    reader.BaseStream.Position += 12;
                    int count = Endianness.Swap(reader.ReadInt32()); // number of samples
                    if (count == 0) count = Endianness.Swap(reader.ReadInt32()); // in some cases a four 0-bytes precede the number of samples
                    long pos = reader.BaseStream.Position;

                    for (int i = 0; i < count; i++)
                    {
                        reader.BaseStream.Position = pos + 24 * i;
                        SampleEntry entry = new SampleEntry();

                        entry.KeyRangeLimit = reader.ReadByte();                            // Upper key range limit
                        reader.BaseStream.Position += 11;
                        entry.Id = Endianness.Swap(reader.ReadInt32());                     // Sample ID
                        reader.BaseStream.Position += 4;
                        entry.FrequenyMultiplier = Endianness.Swap(reader.ReadSingle());    // Frequency multiplier

                        inst.Samples.Add(entry);
                    }

                    reader.BaseStream.Position = pos + count * 24 + 4;
                    inst.FrequencyMultiplier = Endianness.Swap(reader.ReadSingle());        // Frequency multiplier for all samples

                    return inst;
                }
                
                // The entry is a percussion instrument
                else if (chunkId == (uint)ChunkIdentifiers.PercEntry)
                {
                    Instrument inst = new Instrument(true);

                    int count = Endianness.Swap(reader.ReadInt32());
                    long pos = reader.BaseStream.Position;

                    for (int i = 0; i < count; i++)
                    {
                        reader.BaseStream.Position = pos + 4 * i;
                        int offset = Endianness.Swap(reader.ReadInt32());
                        if (offset == 0) continue;

                        reader.BaseStream.Position = offset;
                        if (Endianness.Swap(reader.ReadUInt32()) != (uint)ChunkIdentifiers.PmapEntry) throw new Exception("A percussion entry was ecpected.");
                        reader.BaseStream.Position += 4;

                        SampleEntry entry = new SampleEntry();
                        entry.FrequenyMultiplier = Endianness.Swap(reader.ReadSingle());    // Frequency multiplier
                        entry.Pan = reader.ReadByte();                                      // Pan
                        reader.BaseStream.Position += 15;
                        entry.Id = Endianness.Swap(reader.ReadInt32());                     // Sample ID
                        entry.KeyRangeLimit = (byte)i;                                      // Upper key range limit

                        inst.Samples.Add(entry);
                    }

                    return inst;
                }
                else
                {
                    throw new FileFormatException("Neither an instrument nor percussion data was found. The file might be corrupted, truncated or in an unexpected format.");
                }
            }
        }

        /// <summary>
        /// Returns a list of all samples the specified instrument uses.
        /// </summary>
        /// <param name="instrument">Number (index) of the instrument.</param>
        /// <returns>System.Collections.Generic.List containing the sample IDs.</returns>
        public uint[] GetSamples(uint instrument)
        {
            reader.BaseStream.Position = ListOffset + 4 + 4 * instrument;
            reader.BaseStream.Position = Endianness.Swap(reader.ReadUInt32());
            uint chunkId = Endianness.Swap(reader.ReadUInt32());

            if (chunkId == (uint)ChunkIdentifiers.InstEntry)
            {
                reader.BaseStream.Position += 12;
                uint count = Endianness.Swap(reader.ReadUInt32());
                if (count == 0) count = Endianness.Swap(reader.ReadUInt32());
                uint pos = (uint)reader.BaseStream.Position;
                List<uint> samples = new List<uint>();

                for (int i = 0; i < count; i++)
                {
                    reader.BaseStream.Position = pos + 0x18 * i;
                    reader.BaseStream.Position += 0xc;
                    samples.Add(Endianness.Swap(reader.ReadUInt32()));
                }

                return samples.ToArray();
            }
            else if (chunkId == (uint)ChunkIdentifiers.PercEntry)
            {
                uint count = Endianness.Swap(reader.ReadUInt32());
                uint pos = (uint)reader.BaseStream.Position;
                List<uint> samples = new List<uint>();

                for (uint u = 0; u < count; u++)
                {
                    reader.BaseStream.Position = pos + u * 4;
                    uint offs = Endianness.Swap(reader.ReadUInt32());

                    if (offs != 0)
                    {
                        reader.BaseStream.Position = offs;
                        if (Endianness.Swap(reader.ReadUInt32()) != (uint)ChunkIdentifiers.PmapEntry) throw new Exception("\"Pmap\" expected.");
                        reader.BaseStream.Position += 24;
                        samples.Add(Endianness.Swap(reader.ReadUInt32()));
                    }
                }

                return samples.ToArray();
            }
            else
            {
                throw new FileFormatException("Neither an instrument nor percussion data was found. The file might be corrupted, truncated or in an unexpected format.");
            }
        }

        private uint GetChunkPosition(uint chunkID)
        {
            uint pos = 0x20;

            while (pos < reader.BaseStream.Length - 4)
            {
                reader.BaseStream.Position = pos;
                if (Endianness.Swap(reader.ReadInt32()) == chunkID) return pos + 8;
                else
                {
                    uint size = Endianness.Swap(reader.ReadUInt32());
                    if (size % 4 != 0) size += size % 4;
                    pos += size + 8;
                }
            }

            return 0;
        }

        /// <summary>
        /// The ID of the sample bank this instrument bank uses.
        /// </summary>
        public int Wsys { get; private set; }

        private uint ListOffset;

        BinaryReader reader;

        #region FourCCs

        /// <summary>
        /// Represents the FourCC magic number of an instrument bank.
        /// </summary>
        private const uint MagicNumber = 0x49424E4B;

        /// <summary>
        /// Chunk identifiers.
        /// </summary>
        private enum ChunkIdentifiers : uint
        {
            Envt = 0x454e5654,
            Osct = 0x4f534354,
            Rand = 0x52414e44,
            Sens = 0x53454e53,
            Inst = 0x494e5354,
            InstEntry = 0x496e7374,
            Pmap = 0x504d4150,
            PmapEntry = 0x506d6170,
            Perc = 0x50455243,
            PercEntry = 0x50657263,
            List = 0x4c495354
        }

        #endregion
    }
}
