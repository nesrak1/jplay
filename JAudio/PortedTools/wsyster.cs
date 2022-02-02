//original file wsyster.c

using System;
using System.IO;

namespace JAudio.Tools
{
    public class wsyster
    {
        //TODO BROKE!!! File length is too short even tho header's outSize is the correct length
        private static readonly short[,] afcCoef = new short[,]
        {
            {unchecked((short)0x0000),unchecked((short)0x0000)},
            {unchecked((short)0x0800),unchecked((short)0x0000)},
            {unchecked((short)0x0000),unchecked((short)0x0800)},
            {unchecked((short)0x0400),unchecked((short)0x0400)},
            {unchecked((short)0x1000),unchecked((short)0xf800)},
            {unchecked((short)0x0e00),unchecked((short)0xfa00)},
            {unchecked((short)0x0c00),unchecked((short)0xfc00)},
            {unchecked((short)0x1200),unchecked((short)0xf600)},
            {unchecked((short)0x1068),unchecked((short)0xf738)},
            {unchecked((short)0x12c0),unchecked((short)0xf704)},
            {unchecked((short)0x1400),unchecked((short)0xf400)},
            {unchecked((short)0x0800),unchecked((short)0xf800)},
            {unchecked((short)0x0400),unchecked((short)0xfc00)},
            {unchecked((short)0xfc00),unchecked((short)0x0400)},
            {unchecked((short)0xfc00),unchecked((short)0x0000)},
            {unchecked((short)0xf800),unchecked((short)0x0000)}
        };
        
        private static void DumpAFC(BigEndianReader r, int sampleOffset, int sampleLength, int sampleRate, string fileName)
        {
            LittleEndianWriter w = new LittleEndianWriter(new FileStream(fileName, FileMode.Create));

            int outSize = sampleLength / 9 * 16 * 2;
            int outSizePlusHeader = outSize + 8;
            
            //wav header
            w.Write("RIFF");
            w.Write(outSizePlusHeader);
            w.Write("WAVEfmt ");
            w.Write(16);
            w.Write((ushort)1);
            w.Write((ushort)1);
            w.Write(sampleRate);
            w.Write(sampleRate*2);
            w.Write((ushort)2);
            w.Write((ushort)16);
            w.Write("data");
            w.Write(outSize);

            r.Position = sampleOffset;

            byte[] inBuf;
            short hist1 = 0, hist2 = 0;
            for (int i = sampleLength; i >= 9; i -= 9)
            {
                int pos = 0;
                inBuf = r.ReadBytes(9);

                short delta = (short)(1 << ((inBuf[pos] >> 4) & 0xf));
                short index = (short)(inBuf[pos] & 0xf);
                pos++;

                short[] nibbles = new short[16];
                int k;
                for (int j = 0; j < 16; j += 2)
                {
                    k = (inBuf[pos] & 255) >> 4;
                    nibbles[j] = (short)k;
                    k = inBuf[pos] & 255 & 15;
                    nibbles[j + 1] = (short)k;
                    pos++;
                }

                for (int j = 0; j < 16; j++)
                {
                    if (nibbles[j] >= 8)
                        nibbles[j] = (short)(nibbles[j] - 16);
                }

                int sample;
                for (int j = 0; j < 16; j++)
                {
                    sample = (delta * nibbles[j]) << 11;
                    sample += (int)(((long)hist1 * afcCoef[index, 0]) + ((long)hist2 * afcCoef[index, 1]));
                    sample >>= 11;

                    if (sample > 32767)
                    {
                        sample = 32767;
                    }
                    if (sample < -32768)
                    {
                        sample = -32768;
                    }

                    w.Write((short)sample);

                    hist2 = hist1;
                    hist1 = (short)sample;
                }
            }
        }

        public static bool Convert(FileStream input, out string error)
        {
            string dataDir = Path.GetDirectoryName(input.Name);

            BigEndianReader r = new BigEndianReader(input);

            if (r.ReadStringLength(4) != "WSYS")
            {
                error = "WSYS expected";
                return false;
            }

            r.Position += 12; //skip

            r.Position = r.ReadInt32();
            if (r.ReadStringLength(4) != "WINF")
            {
                error = "WINF expected";
                return false;
            }

            int awCount = r.ReadInt32();
            for (int i = 0; i < awCount; i++)
            {
                int awPos = r.ReadInt32();
                long oldPos = r.Position;
                r.Position = awPos;

                string awName = Path.Combine(dataDir, "Waves", r.ReadNullTerminated());
                int awTablePos = awPos + 0x70;
                r.Position = awTablePos;

                int waveCount = r.ReadInt32();
                for (int j = 0; j < waveCount; j++)
                {
                    int wavPos = r.ReadInt32();
                    long oldPos2 = r.Position;
                    r.Position = wavPos;

                    int unknownMagic = r.ReadInt32();
                    byte unknownByte1 = r.ReadByte();
                    int sampleRate = r.ReadUInt16() / 2;
                    byte unknownByte2 = r.ReadByte();

                    int sampleOffset = r.ReadInt32();
                    int sampleLength = r.ReadInt32();
                    int unknownBool = r.ReadInt32();
                    
                    if (File.Exists(awName))
                    {
                        DumpAFC(new BigEndianReader(File.OpenRead(awName)), sampleOffset, sampleLength, sampleRate,
                            Path.Combine(dataDir, "Waves", awName + "_" + j.ToString("x8") + ".wav"));
                    }
                    else
                    {
                        error = awName + " missing, have you extracted the waves folder?";
                        return false;
                    }
                    r.Position = oldPos2;
                }
                r.Position = oldPos;
            }
            error = string.Empty;
            return true;
        }
    }
}
