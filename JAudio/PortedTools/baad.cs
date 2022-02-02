//original file baad.c
/* Parse and dump from BAA (Twilight Princess Audio Archive).
 * This file contains a subfiles which contain audio metadata.
 *
 * I only have one .baa for reference so this may well be wrong, but
 * it seems to be very simple. Simple enough that there isn't a direct
 * listing of the size of certain files, so I use the known order of
 * Z2Sound.baa to determine the limits.
 * In a lot of cases I also assume there is only one of each chunk type
 * when naming the output files.
 */

using System;
using System.IO;

namespace JAudio.Tools
{
    public class baad
    {
        public static bool Convert(FileStream input, string outName)
        {
            BigEndianReader r = new BigEndianReader(input);

            if (r.ReadStringLength(4) != "AA_<")
            {
                Console.WriteLine("format not recognized");
                return false;
            }

            while (true)
            {
                string chunkName = r.ReadStringLength(4);
                if (chunkName == "bst ") 
                {
                    int start = r.ReadInt32();
                    int end = r.ReadInt32();
                    long oldPos = r.Position;
                    r.Position = start;
                    File.WriteAllBytes(outName + ".bst", r.ReadBytes(end - start));
                    r.Position = oldPos;
                }
                else if (chunkName == "bstn")
                {
                    int start = r.ReadInt32();
                    int end = r.ReadInt32();
                    long oldPos = r.Position;
                    r.Position = start;
                    File.WriteAllBytes(outName + ".bstn", r.ReadBytes(end - start));
                    r.Position = oldPos;
                }
                else if (chunkName == "ws  ")
                {
                    int index = r.ReadInt32();
                    int start = r.ReadInt32();
                    int unknown = r.ReadInt32();
                    long oldPos = r.Position;
                    r.Position = start + 4;
                    int length = r.ReadInt32();
                    r.Position -= 8;
                    File.WriteAllBytes(outName + "." + index + ".wsys", r.ReadBytes(length));
                    r.Position = oldPos;
                }
                else if (chunkName == "bnk ")
                {
                    int index = r.ReadInt32();
                    int start = r.ReadInt32();
                    long oldPos = r.Position;
                    r.Position = start + 4;
                    int length = r.ReadInt32();
                    r.Position -= 8;
                    File.WriteAllBytes(outName + "." + index + ".bnk", r.ReadBytes(length));
                    r.Position = oldPos;
                }
                else if (chunkName == "bsc ")
                {
                    int start = r.ReadInt32();
                    int end = r.ReadInt32();
                    long oldPos = r.Position;
                    r.Position = start;
                    File.WriteAllBytes(outName + ".bsc", r.ReadBytes(end - start));
                    r.Position = oldPos;
                }
                else if (chunkName == "bfca")
                {
                    int start = r.ReadInt32();
                    long oldPos = r.Position;
                    r.Position = start + 4;
                    int length = r.ReadInt32();
                    r.Position -= 8;
                    File.WriteAllBytes(outName + ".rarc", r.ReadBytes(length));
                    r.Position = oldPos;
                }
                else if (chunkName == ">_AA")
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("unknown chunk");
                    return false;
                }
            }
        }
    }
}
