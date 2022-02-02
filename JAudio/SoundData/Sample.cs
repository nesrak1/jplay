using System;
using System.Collections.Generic;
using System.Text;

namespace JAudio.SoundData
{
    /// <summary>
    /// An audio sample.
    /// </summary>
    public struct Sample
    {
        private byte _RootKey;
        private int _SampleRate;
        private short _BitsPerSample;
        private short _Channels;
        private bool _IsLooping;
        private int _LoopStart;
        private byte[] _Data;

        /// <summary>
        /// The root key of the sample.
        /// </summary>
        public byte RootKey
        {
            get { return _RootKey; }
            set { if (value < 0x80)_RootKey = value; else throw new ArgumentOutOfRangeException("RootKey", "The root key must be less than or qual to 127."); }
        }

        /// <summary>
        /// The samples per second.
        /// </summary>
        public int SamplesPerSecond
        {
            get { return _SampleRate; }
            set { _SampleRate = value; }
        }

        /// <summary>
        /// The bits per sample.
        /// </summary>
        public short BitsPerSample
        {
            get { return _BitsPerSample; }
            set { _BitsPerSample = value; }
        }

        /// <summary>
        /// The number of channels. (1 = Mono, 2 = Stereo, etc.)
        /// </summary>
        public short Channels
        {
            get { return _Channels; }
            set { _Channels = value; }
        }

        /// <summary>
        /// Indicates whether the sample loops or not.
        /// </summary>
        public bool IsLooping
        {
            get { return _IsLooping; }
            set { _IsLooping = value; }
        }

        /// <summary>
        /// Indicates at which position the loop starts.
        /// </summary>
        public int LoopStart
        {
            get { return _LoopStart; }
            set { _LoopStart = value; }
        }

        /// <summary>
        /// The audio data.
        /// </summary>
        public byte[] Data
        {
            get { return _Data; }
            set { _Data = value; }
        }
    }

    /// <summary>
    /// Exception thrown when a sample cannot be found.
    /// </summary>
    public class SampleNotFoundException : Exception
    {
        public SampleNotFoundException() : base() { }
        public SampleNotFoundException(string message) : base(message) { }
    }
}
