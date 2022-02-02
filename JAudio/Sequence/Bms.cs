using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using JAudio.Utils;

namespace JAudio.Sequence
{
    /// <summary>
    /// Class for reading Nintendo's BMS sequence files.
    /// </summary>
    public partial class Bms
    {
        /// <summary>
        /// Opens a BMS sequence.
        /// </summary>
        /// <param name="stream">Stream to read the BMS from.</param>
        public Bms(Stream stream)
        {
            try
            {
                reader = new BinaryReader(stream);

                int trackListOffset = 0;

                List<int> jumpBack = new List<int>();

                #region Main Header

                MetaTrack = new List<Event>();

                // Loop through the main header
                byte marker = 0xff;
                do
                {
                    marker = reader.ReadByte();
                    switch (marker)
                    {
                        // Track list offset
                        case 0xc1:
                            if (trackListOffset == 0) trackListOffset = Endianness.Swap(reader.ReadInt32());
                            else reader.BaseStream.Position += 4;
                            break;

                        // Articulation
                        case 0xd8:
                            {
                                switch (reader.ReadByte())
                                {
                                    // Time resolution
                                    case 0x62:
                                        MetaTrack.Add(new TimeResolutionEvent(reader.BaseStream.Position, Endianness.Swap(reader.ReadInt16())));
                                        break;

                                    default:
                                        reader.BaseStream.Position += 2;
                                        break;
                                }
                            }
                            break;

                        // Tempo
                        case 0xe0:
                            {
                                MetaTrack.Add(new TempoEvent(reader.BaseStream.Position, Endianness.Swap(reader.ReadInt16())));
                            }
                            break;

                        // Delay
                        case 0xf0:
                            {
                                int size = 1;
                                while (reader.ReadByte() >= 0x80) size++;
                                reader.BaseStream.Position -= size;
                                byte[] data = reader.ReadBytes(size);
                                MetaTrack.Add(new DelayEvent(reader.BaseStream.Position, GetLength(data)));
                            }
                            break;

                        // Loop to offset
                        case 0xc7:
                            {
                                List<byte> data = new List<byte>(4);
                                data.Add(0);
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());

                                byte[] bytes = data.ToArray();
                                Array.Reverse(bytes);
                                int offset = BitConverter.ToInt32(bytes, 0);

                                MetaTrack.Add(new LoopEvent(reader.BaseStream.Position, offset));
                            }
                            break;

                        // Marker
                        case 0xfd:
                            {
                                //throw new DynamicSequenceException();

                                List<byte> chars = new List<byte>();
                                byte b;
                                while (true)
                                {
                                    b = reader.ReadByte();
                                    if (b != 0x0) chars.Add(b);
                                    else break;
                                }

                                ASCIIEncoding enc = new ASCIIEncoding();
                                //MetaTrack.Add(new MarkerEvent(enc.GetString(chars.ToArray())));
                            }
                            break;

                        // Reference
                        case 0xc3:
                            {
                                List<byte> data = new List<byte>(4);
                                data.Add(0);
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());

                                byte[] bytes = data.ToArray();
                                Array.Reverse(bytes);
                                int offset = BitConverter.ToInt32(bytes, 0);

                                jumpBack.Add((int)reader.BaseStream.Position);
                                reader.BaseStream.Position = offset;
                            }
                            break;

                        // Loop call
                        case 0xc4:
                            {
                                reader.ReadByte();

                                List<byte> data = new List<byte>(4);
                                data.Add(0);
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());

                                byte[] bytes = data.ToArray();
                                Array.Reverse(bytes);
                                int offset = BitConverter.ToInt32(bytes, 0);

                                jumpBack.Add((int)reader.BaseStream.Position);
                                reader.BaseStream.Position = offset;
                            }
                            break;

                        // Jump back to reference marker
                        case 0xc5:
                            if (jumpBack.Count > 0)
                            {
                                reader.BaseStream.Position = jumpBack[jumpBack.Count - 1];
                                jumpBack.RemoveAt(jumpBack.Count - 1);
                            }
                            break;

                        // Track end
                        case 0xff:
                            MetaTrack.Add(new TerminateEvent(reader.BaseStream.Position));
                            break;

                        // Unknown markers
                        case 0: break;
                        case 0xc2: reader.BaseStream.Position += 1; break;
                        case 0xb8:
                        case 0xd0:
                        case 0xd1:
                        case 0xd5:
                        case 0xf9: reader.BaseStream.Position += 2; break;
                        case 0xd9: reader.BaseStream.Position += 3; break;
                        case 0xc8:
                        case 0xda: reader.BaseStream.Position += 4; break;
                    }
                }
                while (marker != 0xff);

                #endregion
                
                #region Track List

                reader.BaseStream.Position = trackListOffset;
                TrackList = new List<Event>();
                Tracks = new List<Track>();

                marker = 0xff;
                do
                {
                    marker = reader.ReadByte();
                    switch (marker)
                    {
                        // Track list entry
                        case 0xc1:
                            {
                                byte trackNum = reader.ReadByte();

                                List<byte> data = new List<byte>(4);
                                data.Add(0);
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());

                                byte[] bytes = data.ToArray();
                                Array.Reverse(bytes);
                                int offset = BitConverter.ToInt32(bytes, 0);

                                Tracks.Add(new Track(trackNum, offset));
                            }
                            break;

                        // Delay
                        case 0xf0:
                            {
                                int size = 1;
                                while (reader.ReadByte() >= 0x80) size++;
                                reader.BaseStream.Position -= size;
                                byte[] data = reader.ReadBytes(size);
                                TrackList.Add(new DelayEvent(reader.BaseStream.Position, GetLength(data)));
                            }
                            break;

                        // Loop to offset
                        case 0xc7:
                            {
                                List<byte> data = new List<byte>(4);
                                data.Add(0);
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());
                                data.Add(reader.ReadByte());

                                byte[] bytes = data.ToArray();
                                Array.Reverse(bytes);
                                int offset = BitConverter.ToInt32(bytes, 0);

                                TrackList.Add(new LoopEvent(reader.BaseStream.Position, offset));
                            }
                            break;
                    }
                }
                while (marker != 0xff);

                #endregion

                #region Track Data

                foreach (Track t in Tracks)
                {
                    reader.BaseStream.Position = t.Offset;

                    marker = 0xff;
                    do
                    {
                        marker = reader.ReadByte();

                        if (marker <= 0x80)
                        {
                            // Note on event
                            t.Data.Add(new NoteOnEvent(reader.BaseStream.Position, marker, reader.ReadByte(), reader.ReadByte()));
                        }
                        else if (marker > 0x80 && marker <= 0x8f)
                        {
                            // Note off event
                            t.Data.Add(new NoteOffEvent(reader.BaseStream.Position, (byte)(marker & 0xf)));
                        }
                        else
                        {
                            switch (marker)
                            {
                                // Delay
                                case 0xf0:
                                    {
                                        int size = 1;
                                        while (reader.ReadByte() >= 0x80) size++;
                                        reader.BaseStream.Position -= size;
                                        byte[] data = reader.ReadBytes(size);
                                        t.Data.Add(new DelayEvent(reader.BaseStream.Position, GetLength(data)));
                                    }
                                    break;

                                // Bank select
                                case 0xe2:
                                    t.Data.Add(new BankSelectEvent(reader.BaseStream.Position, reader.ReadByte()));
                                    break;

                                // Instrument change
                                case 0xe3:
                                    t.Data.Add(new InstrumentChangeEvent(reader.BaseStream.Position, reader.ReadByte()));
                                    break;

                                // Global speakers
                                case 0xb8:
                                    {
                                        switch (reader.ReadByte())
                                        {
                                            // Volume change
                                            case 0x0:
                                                t.Data.Add(new VolumeEvent(reader.BaseStream.Position, reader.ReadByte()));
                                                break;

                                            // Reverb
                                            case 0x2:
                                                t.Data.Add(new ReverbEvent(reader.BaseStream.Position, reader.ReadByte()));
                                                break;

                                            // Pan
                                            case 0x3:
                                                t.Data.Add(new PanEvent(reader.BaseStream.Position, reader.ReadByte()));
                                                break;

                                            default:
                                                reader.BaseStream.Position += 1;
                                                break;
                                        }
                                    }
                                    break;

                                // Reference
                                case 0xc3:
                                    {
                                        List<byte> data = new List<byte>(4);
                                        data.Add(0);
                                        data.Add(reader.ReadByte());
                                        data.Add(reader.ReadByte());
                                        data.Add(reader.ReadByte());

                                        byte[] bytes = data.ToArray();
                                        Array.Reverse(bytes);
                                        int offset = BitConverter.ToInt32(bytes, 0);

                                        jumpBack.Add((int)reader.BaseStream.Position);
                                        reader.BaseStream.Position = offset;
                                    }
                                    break;

                                // Loop call
                                case 0xc4:
                                    {
                                        reader.ReadByte();

                                        List<byte> data = new List<byte>(4);
                                        data.Add(0);
                                        data.Add(reader.ReadByte());
                                        data.Add(reader.ReadByte());
                                        data.Add(reader.ReadByte());

                                        byte[] bytes = data.ToArray();
                                        Array.Reverse(bytes);
                                        int offset = BitConverter.ToInt32(bytes, 0);

                                        jumpBack.Add((int)reader.BaseStream.Position);
                                        reader.BaseStream.Position = offset;
                                    }
                                    break;

                                // Loop to offset
                                case 0xc7:
                                    {
                                        List<byte> data = new List<byte>(4);
                                        data.Add(0);
                                        data.Add(reader.ReadByte());
                                        data.Add(reader.ReadByte());
                                        data.Add(reader.ReadByte());

                                        byte[] bytes = data.ToArray();
                                        Array.Reverse(bytes);
                                        int offset = BitConverter.ToInt32(bytes, 0);

                                        t.Data.Add(new LoopEvent(reader.BaseStream.Position, offset));
                                    }
                                    break;

                                // Jump back to reference marker
                                case 0xc5:
                                    if (jumpBack.Count > 0)
                                    {
                                        reader.BaseStream.Position = jumpBack[jumpBack.Count - 1];
                                        jumpBack.RemoveAt(jumpBack.Count - 1);
                                    }
                                    break;
                                
                                // Pitch bend
                                case 0xb9:
                                    {
                                        byte b = reader.ReadByte();
                                        if (b == 0x1)
                                        {
                                            // Pitch bend
                                            t.Data.Add(new PitchEvent(reader.BaseStream.Position, Endianness.Swap(reader.ReadInt16())));
                                        }
                                        else reader.BaseStream.Position += 2;
                                    }
                                    break;

                                // Articulation
                                case 0xd8:
                                    {
                                        switch (reader.ReadByte())
                                        {
                                            // Vibrato
                                            case 0x6e:
                                                t.Data.Add(new VibratoEvent(reader.BaseStream.Position, Endianness.Swap(reader.ReadUInt16())));
                                                break;

                                            default:
                                                reader.BaseStream.Position += 2;
                                                break;
                                        }
                                    }
                                    break;

                                // End of track
                                case 0xff:
                                    t.Data.Add(new TerminateEvent(reader.BaseStream.Position));
                                    break;

                                case 0xf9: reader.BaseStream.Position += 2; break;
                            }
                        }
                    }
                    while (marker != 0xff);
                }

                #endregion

                reader.BaseStream.Close();
            }

            catch (Exception e) { throw e; }
            finally { stream.Close(); }
        }

        #region Properties

        /// <summary>
        /// Meta track containing tempo changes, looping information, etc.
        /// </summary>
        public List<Event> MetaTrack { get; private set; }

        /// <summary>
        /// Track list events.
        /// </summary>
        public List<Event> TrackList { get; private set; }

        /// <summary>
        /// Track list.
        /// </summary>
        public List<Track> Tracks { get; private set; }

        #endregion

        private BinaryReader reader;

        /// <summary>
        /// Convert the delay data to ticks.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int GetLength(byte[] data)
        {
            int length = 0;

            for (uint u = 0; u < data.Length; u++)
            {
                length += (int)(Math.Pow(0x80, u) * (data[data.Length - 1 - u] - (u == 0 ? 0 : 0x80)));
            }

            return length;
        }
    }
}
