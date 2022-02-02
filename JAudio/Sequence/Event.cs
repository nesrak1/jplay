using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JAudio.Sequence
{
    /// <summary>
    /// Class representing a general BMS event.
    /// </summary>
    public abstract class Event
    {
        // This constructor will later require the event offset (to support looping).
        //done haha
        public Event(long eventOffset) { EventOffset = (int)eventOffset; }

        public int EventOffset { get; set; }
    }

    /// <summary>
    /// Delay event.
    /// </summary>
    public class DelayEvent : Event
    {
        public DelayEvent(long eventOffset, int delay) : base(eventOffset) { Delay = delay; }

        public int Delay { get; set; }
    }

    /// <summary>
    /// Time resolution event.
    /// </summary>
    public class TimeResolutionEvent : Event
    {
        public TimeResolutionEvent(long eventOffset, short resolution) : base(eventOffset) { TimeResolution = resolution; }

        public short TimeResolution { get; set; }
    }

    /// <summary>
    /// Tempo event.
    /// </summary>
    public class TempoEvent : Event
    {
        public TempoEvent(long eventOffset, short tempo) : base(eventOffset) { Tempo = tempo; }

        public short Tempo { get; set; }
    }

    /// <summary>
    /// Marker event.
    /// </summary>
    public class MarkerEvent : Event
    {
        public MarkerEvent(long eventOffset, string text) : base(eventOffset) { Text = text; }

        public string Text { get; set; }
    }

    /// <summary>
    /// Loop event.
    /// </summary>
    public class LoopEvent : Event
    {
        public LoopEvent(long eventOffset, int offset) : base(eventOffset) { Offset = offset; }

        public int Offset { get; set; }
    }

    /// <summary>
    /// Instrument change event.
    /// </summary>
    public class InstrumentChangeEvent : Event
    {
        public InstrumentChangeEvent(long eventOffset, byte instrument) : base(eventOffset) { Instrument = instrument; }

        public byte Instrument { get; set; }
    }

    /// <summary>
    /// Bank select event.
    /// </summary>
    public class BankSelectEvent : Event
    {
        public BankSelectEvent(long eventOffset, byte bank) : base(eventOffset) { Bank = bank; }

        public byte Bank { get; set; }
    }

    /// <summary>
    /// Volume change event.
    /// </summary>
    public class VolumeEvent : Event
    {
        public VolumeEvent(long eventOffset, byte volume) : base(eventOffset) { Volume = volume; }

        public byte Volume { get; set; }
    }

    /// <summary>
    /// Reverb change event.
    /// </summary>
    public class ReverbEvent : Event
    {
        public ReverbEvent(long eventOffset, byte reverb) : base(eventOffset) { Reverb = reverb; }

        public byte Reverb { get; set; }
    }

    /// <summary>
    /// Pan change event.
    /// </summary>
    public class PanEvent : Event
    {
        public PanEvent(long eventOffset, byte pan) : base(eventOffset) { Pan = pan; }

        public byte Pan { get; set; }
    }

    /// <summary>
    /// Note on event.
    /// </summary>
    public class NoteOnEvent : Event
    {
        public NoteOnEvent(long eventOffset, byte note, byte number, byte velocity) : base(eventOffset) { Note = note; Number = number; Velocity = velocity; }

        public byte Note { get; set; }
        public byte Number { get; set; }
        public byte Velocity { get; set; }
    }

    /// <summary>
    /// Note off event.
    /// </summary>
    public class NoteOffEvent : Event
    {
        public NoteOffEvent(long eventOffset, byte number) : base(eventOffset) { Number = number; }

        public byte Number { get; set; }
    }

    /// <summary>
    /// Pitch bend event.
    /// </summary>
    public class PitchEvent : Event
    {
        public PitchEvent(long eventOffset, short pitch) : base(eventOffset) { Pitch = pitch; }

        public short Pitch { get; set; }
    }

    /// <summary>
    /// Vibrato event.
    /// </summary>
    public class VibratoEvent : Event
    {
        public VibratoEvent(long eventOffset, ushort vibrato) : base(eventOffset) { Vibrato = vibrato; }

        public ushort Vibrato { get; set; }
    }

    /// <summary>
    /// Event indicating the end of the track.
    /// </summary>
    public class TerminateEvent : Event
    {
        public TerminateEvent(long eventOffset) : base(eventOffset) { }
    }

    /// <summary>
    /// Text event.
    /// </summary>
    public class TextEvent : Event
    {
        public TextEvent(long eventOffset, string text) : base(eventOffset) { Text = text; }

        public string Text { get; set; }
    }
}
