using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JAudio;
using JAudio.Sequence;
using JAudio.SoundData;
using SharpDX.XAudio2;
using Multimedia;

namespace JAudioPlayer
{
    /// <summary>
    /// Class for the playback of JAudio sequences.
    /// </summary>
    partial class Playback
    {
        /// <summary>
        /// Initializes a new instance of the Playback class.
        /// </summary>
        public Playback(string soundPath)
        {
            Z2Sound.SoundPath = soundPath;
            soundData = new Z2Sound(System.IO.File.OpenRead(soundPath + "\\Z2Sound.baa"));
            musicEngine = new XAudio2();
            masteringVoice = new MasteringVoice(musicEngine);

            timer = new Timer();
            timer.Period = 1;
            timer.Resolution = 1;
            timer.Tick += new EventHandler(HandleTick);

            IsPlaying = false;

            DebugText = "";
        }

        public Playback(string soundPath, Action tickCallback) : this(soundPath)
        {
            TickUpdate = tickCallback;
        }

        /// <summary>
        /// Class destructor.
        /// </summary>
        ~Playback()
        {
            if (IsPlaying)
            {
                Stop();
            }
        }

        /// <summary>
        /// Starts the playback of the specified sequence.
        /// </summary>
        public void Start()
        {
            if (Sequence == null) throw new InvalidOperationException("A sequence was not specified.");
            IsPlaying = true;

            timer.Start();
        }

        /// <summary>
        /// Stops the playback.
        /// </summary>
        public void Stop()
        {
            if (!IsPlaying)
                return;

            timer.Stop();

            foreach (Track t in tracks)
            {
                t.AllNotesOff();
            }

            GC.Collect();
            IsPlaying = false;
            Time = 0;
            FractionalTicks = 0;
        }

        public bool IsPlaying { get; set; }

        /// <summary>
        /// The JAudio sequence to be played.
        /// </summary>
        public Bms Sequence
        {
            get { return sequence; }
            set
            {
                if (IsPlaying) throw new InvalidOperationException("The sequence cannot be set during playback.");
                else sequence = value;
                Time = 0;
                FractionalTicks = 0;
                TicksPerBeat = 120;
                Tempo = 120;
                Preload();
            }
        }

        /// <summary>
        /// Caches all instrument and sample data.
        /// </summary>
        private void Preload()
        {
            List<Tuple<byte, byte>> instruments = new List<Tuple<byte, byte>>();
            trackData = new List<TrackData>();
            trackData.Add(new TrackData());
            tracks = new List<Track>(Sequence.Tracks.Count);
            instrumentTable = new Dictionary<Tuple<byte, byte>, Instrument>();
            sampleTable = new Dictionary<int, Sample>();

            // List all instruments that are used by the sequence.
            foreach (JAudio.Sequence.Track t in Sequence.Tracks)
            {
                trackData.Add(new TrackData());
                tracks.Add(new Track(musicEngine, soundData, instrumentTable, sampleTable));
                byte bank = 0;

                foreach (Event e in t.Data)
                {
                    if (e is BankSelectEvent)
                    {
                        bank = ((BankSelectEvent)e).Bank;
                    }
                    else if (e is InstrumentChangeEvent)
                    {
                        instruments.Add(new Tuple<byte, byte>(bank, ((InstrumentChangeEvent)e).Instrument));
                    }
                }
            }

            instruments = instruments.Distinct().ToList();

            // Cache all samples
            foreach (Tuple<byte, byte> entry in instruments)
            {
                if (entry.Item1 == 0) continue;

                Instrument inst = soundData.InstrumentBanks[entry.Item1][entry.Item2];
                instrumentTable.Add(entry, inst);

                foreach (SampleEntry s in inst.Samples)
                {
                    try
                    {
                        if (!sampleTable.ContainsKey(s.Id)) sampleTable.Add(s.Id, soundData.SampleBanks[soundData.InstrumentBanks[entry.Item1].Wsys][(short)s.Id]);
                    }
                    catch (SampleNotFoundException)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Handles tick events from the multimedia timer.
        /// </summary>
        private void HandleTick(Object sender, EventArgs e)
        {
            int t = GenerateTicks();

            for (int i = 0; i < t; i++)
            {
                OnTick();
            }
        }

        /// <summary>
        /// The actual sequence tick event.
        /// </summary>
        private void OnTick()
        {
            TickUpdate?.Invoke();
            ++Time;

            // Process meta track
            if (Time - 1 == trackData[0].NextEventTime)
            {
                bool setEveIdx = false;

                int i = trackData[0].EventIndex - 1;
                while (i < Sequence.MetaTrack.Count - 1)
                {
                    ++i;
                    Event evt = Sequence.MetaTrack[i];

                    if (evt is DelayEvent)
                    {
                        trackData[0].NextEventTime += (int)(evt as DelayEvent).Delay;
                        break;
                    }
                    else if (evt is LoopEvent) //usually occurs in the "other tracks" section
                    {
                        int newOffset = (evt as LoopEvent).Offset;
                        for (int j = 0; j < Sequence.Tracks[0].Data.Count; j++)
                        {
                            Event evt2 = Sequence.Tracks[0].Data[j];
                            if (evt2.EventOffset == newOffset)
                            {
                                trackData[0].NextEventTime = Time + 1;
                                trackData[0].EventIndex = j;
                                setEveIdx = true;
                                break;
                            }
                        }

                        if (setEveIdx)
                            break;
                    }
                    else if (evt is TerminateEvent)
                    {
                        //Stop();
                        //return;
                    }
                    else if (evt is TimeResolutionEvent)
                    {
                        TicksPerBeat = (evt as TimeResolutionEvent).TimeResolution;
                    }
                    else if (evt is TempoEvent)
                    {
                        Tempo = ((TempoEvent)evt).Tempo;
                    }
                }

                if (!setEveIdx)
                    trackData[0].EventIndex = i + 1;
            }

            // Process other tracks
            for (int index = 0; index < Sequence.Tracks.Count; index++)
            {
                if (Time - 1 == trackData[index + 1].NextEventTime)
                {
                    bool setEveIdx = false;

                    int i = trackData[index + 1].EventIndex - 1;
                    while (i < Sequence.Tracks[index].Data.Count - 1)
                    {
                        ++i;
                        Event evt = Sequence.Tracks[index].Data[i];

                        if (evt is DelayEvent)
                        {
                            trackData[index + 1].NextEventTime += (int)(evt as DelayEvent).Delay;
                            break;
                        }
                        else if (evt is BankSelectEvent)
                        {
                            tracks[index].Bank = (evt as BankSelectEvent).Bank;
                        }
                        else if (evt is InstrumentChangeEvent)
                        {
                            tracks[index].Instrument = (evt as InstrumentChangeEvent).Instrument;
                        }
                        else if (evt is PanEvent)
                        {
                            tracks[index].Pan = (evt as PanEvent).Pan;
                        }
                        else if (evt is NoteOnEvent)
                        {
                            tracks[index].NoteOn((evt as NoteOnEvent).Number, (evt as NoteOnEvent).Note, (evt as NoteOnEvent).Velocity);
                        }
                        else if (evt is NoteOffEvent)
                        {
                            tracks[index].NoteOff((evt as NoteOffEvent).Number);
                        }
                        else if (evt is VolumeEvent)
                        {
                            tracks[index].Volume = (evt as VolumeEvent).Volume;
                        }
                        else if (evt is ReverbEvent)
                        {
                            tracks[index].Echo = (evt as ReverbEvent).Reverb;
                        }
                        else if (evt is PitchEvent)
                        {
                            tracks[index].Pitch = (evt as PitchEvent).Pitch;
                        }
                        else if (evt is LoopEvent)
                        {
                            int newOffset = (evt as LoopEvent).Offset;
                            for (int j = 0; j < Sequence.Tracks[index].Data.Count; j++)
                            {
                                Event evt2 = Sequence.Tracks[index].Data[j];
                                if (evt2.EventOffset == newOffset)
                                {
                                    tracks[index].AllNotesOff();
                                    trackData[index + 1].NextEventTime = Time + 1;
                                    //for whatever reason, the index we get is the delay event
                                    //BEFORE the actual index we need to go to, so we +1 here.
                                    trackData[index + 1].EventIndex = j + 1;
                                    setEveIdx = true;
                                    break;
                                }
                            }

                            if (setEveIdx)
                                break;
                        }
                        //else if (evt is TerminateEvent)
                        //{
                        //    Stop();
                        //    return;
                        //}
                    }

                    if (!setEveIdx)
                        trackData[index + 1].EventIndex = i + 1;
                }

                // Update track
                tracks[index].Update((int)Math.Round(60000D / TicksPerBeat / Tempo));
            }
        }

        /// <summary>
        /// Helper function for handling fractional ticks.
        /// </summary>
        private int GenerateTicks()
        {
            int periodResolution = 1000 * TicksPerBeat * timer.Period;
            int tempo = 60000000 / Tempo;

            int ticks = (FractionalTicks + periodResolution) / tempo;
            FractionalTicks += periodResolution - ticks * tempo;

            return ticks;
        }

        public int Time { get; set; }
        public int FractionalTicks { get; set; }
        public int TicksPerBeat { get; set; }
        public int Tempo { get; set; }
        public string DebugText { get; set; }

        private Z2Sound soundData;
        private MasteringVoice masteringVoice;
        private XAudio2 musicEngine;
        public Timer timer;
        private Bms sequence;

        public Action TickUpdate;

        public List<TrackData> trackData;
        public List<Track> tracks;
        private Dictionary<Tuple<byte, byte>, Instrument> instrumentTable;
        private Dictionary<int, Sample> sampleTable;

        /// <summary>
        /// Track data for playback.
        /// </summary>
        public class TrackData
        {
            /// <summary>
            /// Time of the next event in ticks.
            /// </summary>
            public int NextEventTime { get; set; }

            /// <summary>
            /// Eventindex.
            /// </summary>
            public int EventIndex { get; set; }
        }
    }
}
