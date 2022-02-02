using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JAudio;
using JAudio.SoundData;
using SharpDX;
using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using SharpDX.XAPO;
using SharpDX.XAPO.Fx;

namespace JAudioPlayer
{
    partial class Playback
    {
        /// <summary>
        /// Playback track.
        /// </summary>
        public class Track
        {
            /// <summary>
            /// Initializes a new instance of this class.
            /// </summary>
            public Track(XAudio2 xAudio2, Z2Sound baa, Dictionary<Tuple<byte, byte>, Instrument> instrumentTable, Dictionary<int, Sample> sampleTable)
            {
                this.engine = xAudio2;
                this.soundData = baa;
                this.instrumentTable = instrumentTable;
                this.sampleTable = sampleTable;
                voiceTable = new Dictionary<byte, NoteData>();

                submix = new SubmixVoice(engine);

                Echo echo = new SharpDX.XAPO.Fx.Echo();

                EffectDescriptor desc = new EffectDescriptor(echo);
                desc.InitialState = true;
                desc.OutputChannelCount = 2;
                submix.SetEffectChain(desc);

                EchoParameters p = new EchoParameters();
                p.Delay = 160;
                p.Feedback = 0.0f;
                p.WetDryMix = 0.0f;

                submix.SetEffectParameters<EchoParameters>(0, p);
                submix.EnableEffect(0);

                outputMatrix = new float[2];

                Volume = 0x7f;
                Pitch = 0;
                Pan = 0x3f;
                panChanged = false;
                Bank = 0;
                Echo = 0;
                echoChanged = false;
                Instrument = 0;

                outputMatrix[0] = outputMatrix[1] = 0.5f;
            }

            /// <summary>
            /// Updates controller and envelope data.
            /// </summary>
            /// <param name="deltaMs">The elapsed time in milliseconds.</param>
            public void Update(int deltaMs)
            {
                foreach (KeyValuePair<byte, NoteData> v in voiceTable)
                {
                    v.Value.Voice.SetFrequencyRatio(v.Value.FrequencyRatio * PitchFrequencyRatio);
                    //v.Value.Voice.SetVolume((float)(Volume / 128f) * v.Value.Velocity);

                    //if (v.Value.IsPercussion)
                    //{
                    //    sbyte pan = (sbyte)(v.Value.PercussionPan - 0x3f);
                    //    float[] matrix = new float[2];
                    //    matrix[0] = 0.5f - (float)pan / 128f;
                    //    matrix[1] = 0.5f + (float)pan / 128f;
                    //    v.Value.Voice.SetOutputMatrix(1, 2, matrix);
                    //    v.Value.Voice.SetVolume(v.Value.Velocity * (float)(Volume / 128f));
                    //}

                    if (panChanged && !v.Value.IsPercussion)
                    {
                        v.Value.Voice.SetOutputMatrix(1, 2, outputMatrix);
                    }
                }

                submix.SetVolume(ValueToAmplitude(Volume), 0);

                if (panChanged) panChanged = false;

                if (echoChanged)
                {
                    EchoParameters p = new EchoParameters();
                    p.Delay = 160;
                    p.Feedback = 0.6f;
                    p.WetDryMix = (float)(Echo / 384f);
                    submix.SetEffectParameters<EchoParameters>(0, p);

                    echoChanged = false;
                }

                // TODO: Envelope data
            }

            /// <summary>
            /// Starts playing a note.
            /// </summary>
            /// <param name="num">Number of the note. Neccessary for the NoteOff method.</param>
            /// <param name="key">The key of the note.</param>
            /// <param name="velocity">The velocity of the note.</param>
            public void NoteOn(byte num, byte key, byte velocity)
            {
                JAudio.SoundData.Instrument inst;
                Sample s;

                if (!Enabled)
                    return;

                try
                {
                    inst = instrumentTable[new Tuple<byte, byte>(Bank, Instrument)];
                    int index = (int)inst.GetSampleEntry(key);
                    s = soundData.SampleBanks[soundData.InstrumentBanks[Bank].Wsys][(short)inst.Samples[index].Id];

                    AudioBuffer buf = new AudioBuffer();
                    buf.AudioBytes = s.Data.Length;
                    buf.Stream = new DataStream(s.Data.Length, true, true);
                    buf.Stream.Write(s.Data, 0, s.Data.Length);

                    buf.PlayBegin = 0;
                    buf.PlayLength = 0;
                    buf.LoopCount = s.IsLooping ? AudioBuffer.LoopInfinite : 0;
                    buf.LoopBegin = (int)s.LoopStart;
                    buf.LoopLength = 0;

                    var format = new SharpDX.Multimedia.WaveFormat((int)s.SamplesPerSecond, (int)s.BitsPerSample, (int)s.Channels);
                    SourceVoice v = new SourceVoice(engine, format, VoiceFlags.None, XAudio2.MaximumFrequencyRatio);

                    v.SetOutputVoices(new VoiceSendDescriptor(submix));
                    v.SubmitSourceBuffer(buf, null);

                    float freqRatio = XAudio2.SemitonesToFrequencyRatio(inst.IsPercussion ? 0 : key - s.RootKey) * inst.Samples[index].FrequenyMultiplier * inst.FrequencyMultiplier;
                    v.SetFrequencyRatio(freqRatio);
                    v.SetVolume(ValueToAmplitude(velocity), 0);

                    NoteData data = new NoteData(v, freqRatio, velocity, inst.IsPercussion);

                    if (inst.IsPercussion)
                    {
                        data.PercussionPan = (byte)inst.Samples[index].Pan;

                        sbyte pan = (sbyte)(data.PercussionPan - 0x3f);
                        float[] matrix = new float[2];
                        matrix[0] = 0.5f - (float)pan / 127f;
                        matrix[1] = 0.5f + (float)pan / 127f;

                        v.SetOutputMatrix(1, 2, matrix);
                    }
                    else v.SetOutputMatrix(1, 2, outputMatrix);

                    data.BufferStream = buf.Stream;
                    data.Voice.Start();
                    voiceTable.Add(num, data);

                    format = null;
                    GC.Collect();
                }
                catch
                {
                }
            }

            /// <summary>
            /// Stops playing a note.
            /// </summary>
            /// <param name="num">The number of the note to be stopped.</param>
            public void NoteOff(byte num)
            {
                if (voiceTable.ContainsKey(num))
                {
                    voiceTable[num].Voice.Stop();
                    voiceTable[num].Voice.DestroyVoice();
                    voiceTable[num].Voice.Dispose();
                    voiceTable[num].BufferStream.Dispose();
                    voiceTable[num] = null;
                    voiceTable.Remove(num);
                }
            }

            /// <summary>
            /// Stop all notes.
            /// </summary>
            public void AllNotesOff()
            {
                foreach (byte num in voiceTable.Keys.ToList())
                {
                    NoteOff(num);
                }
            }

            /// <summary>
            /// Converts velocity/volume to an amplitude multiplier.
            /// </summary>
            /// <param name="val">BMS velocity/volume value.</param>
            /// <returns>Amplitude multiplier.</returns>
            private static float ValueToAmplitude(byte value)
            {
                if (value > 127) throw new ArgumentOutOfRangeException("value", "The velocity cannot be greater than 127.");
                return (float)Math.Pow((float)(value / 127f), 2);
            }

            /// <summary>
            /// The ID of the instrument bank currently used.
            /// </summary>
            public byte Bank { get; set; }

            /// <summary>
            /// The number of the instrument currently used.
            /// </summary>
            public byte Instrument { get; set; }

            /// <summary>
            /// The volume of the track.
            /// </summary>
            public byte Volume { get; set; }

            /// <summary>
            /// If this track is enabled or not.
            /// </summary>
            public bool Enabled { get; set; } = true;

            /// <summary>
            /// The position (pan) of the track.
            /// </summary>
            public byte Pan
            {
                get { return _Pan; }
                set
                {
                    _Pan = value;
                    sbyte pan = (sbyte)(_Pan - 0x3f);

                    outputMatrix[0] = 0.5f - (float)pan / 128f;
                    outputMatrix[1] = 0.5f + (float)pan / 128f;

                    //stereoMatrix[0] = pan <= 0 ? 0.5f : (1 - pan / 64f) / 2;
                    //stereoMatrix[1] = pan >= 0 ? 0.0f : (pan / -64f) / 2;
                    //stereoMatrix[2] = pan <= 0 ? 0.0f : (pan / 64f) / 2;
                    //stereoMatrix[3] = pan >= 0 ? 0.5f : (1 - pan / -64f) / 2;

                    panChanged = true;
                }
            }

            /// <summary>
            /// The reverb (echo) of the track.
            /// </summary>
            public byte Echo
            {
                get { return _Echo; }
                set
                {
                    _Echo = value;
                    echoChanged = true;
                }
            }

            /// <summary>
            /// The pitch value of the track.
            /// </summary>
            public short Pitch
            {
                get { return _Pitch; }
                set
                {
                    _Pitch = value;

                    double val = Convert.ToDouble(value);
                    if (val > Math.Pow(2, 15)) val -= Math.Pow(2, 16);

                    double semitones = val / (682D + 2D / 3D);
                    PitchFrequencyRatio = XAudio2.SemitonesToFrequencyRatio((float)semitones);
                }
            }

            private float PitchFrequencyRatio { get; set; }

            private float[] outputMatrix;
            //private float[] stereoMatrix;
            private byte _Pan;
            private bool panChanged;
            private short _Pitch;
            private byte _Echo;
            private bool echoChanged;

            private readonly XAudio2 engine;
            private readonly SubmixVoice submix;
            private readonly Z2Sound soundData;
            private readonly Dictionary<Tuple<byte, byte>, Instrument> instrumentTable;
            private readonly Dictionary<int, Sample> sampleTable;
            private Dictionary<byte, NoteData> voiceTable;

            /// <summary>
            /// Note data.
            /// </summary>
            private class NoteData
            {
                /// <summary>
                /// Creates a new instance of this class.
                /// </summary>
                /// <param name="v">The XAudio2 SourceVoice.</param>
                /// <param name="velocity">The velocity of the note.</param>
                /// <param name="perc">Value indicating whether the note is percussion or not.</param>
                public NoteData(SourceVoice v, float freqRatio, byte velocity = 127, bool perc = false)
                {
                    if (velocity > 127) throw new ArgumentOutOfRangeException("velocity", "Velocity cannot be greater than 127.");
                    Voice = v;
                    FrequencyRatio = freqRatio;
                    Velocity = 0;
                    IsPercussion = perc;
                    PercussionPan = 0x3f;

                    FadeVolume = 1.0f;
                    State = VoiceState.Active;
                }

                /// <summary>
                /// XAudio2 SourceVoice.
                /// </summary>
                public SourceVoice Voice { get; private set; }

                /// <summary>
                /// Reference to the DataStream of the source voice.
                /// </summary>
                public DataStream BufferStream { get; set; }

                /// <summary>
                /// Velocity of the note.
                /// </summary>
                public float Velocity { get; private set; }

                /// <summary>
                /// The pan (position) of the sample. Only used for percussion.
                /// </summary>
                public byte PercussionPan { get; set; }

                /// <summary>
                /// Original frequency ratio.
                /// </summary>
                public float FrequencyRatio { get; private set; }

                /// <summary>
                /// Determines whether the sample is percussion or not.
                /// </summary>
                public bool IsPercussion { get; private set; }

                /// <summary>
                /// Fade volume.
                /// </summary>
                public float FadeVolume { get; set; }

                /// <summary>
                /// Voice state.
                /// </summary>
                public VoiceState State { get; set; }

                public enum VoiceState
                {
                    Active,
                    Attack,
                    Release,
                    Disposed
                }
            }
        }
    }
}
