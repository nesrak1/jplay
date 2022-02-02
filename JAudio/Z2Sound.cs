using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using JAudio.SoundData;
using JAudio.Utils;

namespace JAudio
{
    /// <summary>
    /// The Legend of Zelda: Twilight princess sound data.
    /// </summary>
    public class Z2Sound
    {
        /// <summary>
        /// Initializes a new isntance of the Z2Sound class.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        public Z2Sound(Stream stream)
        {
            try
            {
                // Open the stream for binary reading
                reader = new BinaryReader(stream);

                // Check whether the stream is a valid sound data file and throw an exception if not
                if (Endianness.Swap(reader.ReadUInt32()) != MagicNumber) throw new FileFormatException();

                // Initialize the sample bank and instrument bank dictionary
                SampleBanks = new Dictionary<int, SampleBank>();
                InstrumentBanks = new Dictionary<int, InstrumentBank>();

                // Loop through the entries

                bool parsed = false;
                while (!parsed)
                {
                    switch ((FileIdentifiers)Endianness.Swap(reader.ReadUInt32()))
                    {
                        case FileIdentifiers.Bst:
                        case FileIdentifiers.Bstn:
                        case FileIdentifiers.Bsc:
                            // There is no implementation for these entries yet.
                            reader.BaseStream.Position += 8;
                            break;

                        case FileIdentifiers.Wsys:
                            {
                                long pos = reader.BaseStream.Position + 12;
                                int id = Endianness.Swap(reader.ReadInt32());
                                long offset = Endianness.Swap(reader.ReadInt32());
                                reader.BaseStream.Position = offset + 4;
                                long length = Endianness.Swap(reader.ReadInt32());
                                reader.BaseStream.Position = pos;

                                // Add the sample bank to the dictionary with its ID as the key
                                var vs = new VirtualStream(reader.BaseStream, offset, length);
                                SampleBanks.Add(id, new SampleBank(vs));
                            }
                            break;

                        case FileIdentifiers.Bank:
                            {
                                long pos = reader.BaseStream.Position + 8;
                                int wsys = Endianness.Swap(reader.ReadInt32());
                                long offset = Endianness.Swap(reader.ReadInt32());
                                reader.BaseStream.Position = offset + 4;
                                long length = Endianness.Swap(reader.ReadInt32());
                                int id = Endianness.Swap(reader.ReadInt32());
                                reader.BaseStream.Position = pos;

                                // Add the instrument bank to the dictionary with its ID as the key
                                InstrumentBanks.Add(id, new InstrumentBank(new VirtualStream(reader.BaseStream, offset, length), wsys));
                            }
                            break;

                        case FileIdentifiers.Bfca:
                            reader.BaseStream.Position += 4;
                            break;

                        case FileIdentifiers.HeaderEnd:
                        default:
                            parsed = true;
                            break;
                    }
                }
            }

            catch (EndOfStreamException)
            {
                throw new FileFormatException();
            }

            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Class destructor.
        /// </summary>
        ~Z2Sound()
        {
            reader.BaseStream.Close();
        }

        /// <summary>
        /// Sample bank dictionary.
        /// </summary>
        public Dictionary<int, SampleBank> SampleBanks;

        /// <summary>
        /// Instrument bank dictionary.
        /// </summary>
        public Dictionary<int, InstrumentBank> InstrumentBanks;

        private BinaryReader reader;

        /// <summary>
        /// The path of the encoded sound files.
        /// </summary>
        public static string SoundPath;

        #region FourCCs

        /// <summary>
        /// Represents the FourCC magic number of the sound data file.
        /// </summary>
        private const uint MagicNumber = 0x41415f3c;

        /// <summary>
        /// Identifiers indicating the content of an entry.
        /// </summary>
        private enum FileIdentifiers : uint
        {
            HeaderEnd = 0x3e5f4141,
            Bst = 0x62737420,
            Bstn = 0x6273746e,
            Wsys = 0x77732020,
            Bank = 0x626e6b20,
            Bsc = 0x62736320,
            Bfca = 0x62666361
        }

        #endregion
    }
}
