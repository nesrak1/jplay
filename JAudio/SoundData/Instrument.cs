using System;
using System.Collections.Generic;
using System.Text;

namespace JAudio.SoundData
{
    /// <summary>
    /// Represents a virtual instrument.
    /// </summary>
    public class Instrument
    {
        /// <summary>
        /// Initializes a new instance of the Instrument class.
        /// </summary>
        public Instrument(bool isPercussion = false)
        {
            IsPercussion = isPercussion;
            Samples = new List<SampleEntry>();

            FrequencyMultiplier = 1.0f;

            AttackTime = 0.0f;
            ReleaseTime = 0.2f;                                                                                                             // Temporary !
        }

        /// <summary>
        /// Retrieves the index of the sample entry which includes the specified key.
        /// </summary>
        /// <param name="key">The key the sample entry includes.</param>
        /// <returns>Index of the sample entry, null when the instrument has no entries.</returns>
        public int? GetSampleEntry(byte key)
        {
            for (int i = 0; i < Samples.Count; i++)
            {
                if (!IsPercussion)
                {
                    if (Samples[i].KeyRangeLimit >= key) return i;
                }
                else
                {
                    if (Samples[i].KeyRangeLimit == key) return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Indicates whether this is an percussion instrument or not.
        /// </summary>
        public bool IsPercussion { get; private set; }

        /// <summary>
        /// Sample entries.
        /// </summary>
        public List<SampleEntry> Samples { get; internal set; }

        /// <summary>
        /// Value with which all sample frequenies are multiplied.
        /// </summary>
        public float FrequencyMultiplier { get; internal set; }

        public float AttackTime { get; internal set; }
        public float ReleaseTime { get; internal set; }
    }

    /// <summary>
    /// Represents a sample entry in an instrument.
    /// </summary>
    public struct SampleEntry
    {
        private byte _KeyRangeLimit;
        private int _Id;
        private float _FrequencyMultiplier;
        private byte? _Pan;

        /// <summary>
        /// Upper key limit at which this sample is used.
        /// </summary>
        public byte KeyRangeLimit
        {
            get { return _KeyRangeLimit; }
            set { if (value < 0x80) _KeyRangeLimit = value; else throw new ArgumentOutOfRangeException("KeyRangeLimit", "Value must be less than or equal to 127."); }
        }

        /// <summary>
        /// Sample ID.
        /// </summary>
        public int Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        /// <summary>
        /// Value with which the sample frequeny is multiplied.
        /// </summary>
        public float FrequenyMultiplier
        {
            get { return _FrequencyMultiplier; }
            set { _FrequencyMultiplier = value; }
        }

        /// <summary>
        /// Sample Pan. Only used in percussion samples/instruments.
        /// </summary>
        public byte? Pan
        {
            get { return _Pan; }
            set { if (value < 0x80) _Pan = value; else throw new ArgumentOutOfRangeException("Pan", "Value must be less than or equal to 127."); }
        }
    }
}
