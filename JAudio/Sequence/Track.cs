using System;
using System.Collections.Generic;
using System.Text;

namespace JAudio.Sequence
{
    /// <summary>
    /// Represents a track in the sequence.
    /// </summary>
    public class Track
    {
        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="trackNumber">Index of the track in the sequence.</param>
        /// <param name="offset">The starting offset of the track.</param>
        public Track(byte trackNumber, int offset)
        {
            TrackNumber = trackNumber;
            Offset = offset;
            Data = new List<Event>();
        }

        /// <summary>
        /// Number of the track in the sequence.
        /// </summary>
        public byte TrackNumber { get; internal set; }

        /// <summary>
        /// Main data event list.
        /// </summary>
        public List<Event> Data { get; internal set; }

        /// <summary>
        /// Offset of the track data in the BMS.
        /// </summary>
        internal int Offset { get; private set; }
    }
}
