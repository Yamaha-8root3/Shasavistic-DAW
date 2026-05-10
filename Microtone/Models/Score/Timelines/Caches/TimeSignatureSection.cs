using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.Caches
{
    internal struct TimeSignatureSection
    {
        public long StartTick { get; init; }
        public long EndTick { get; init; }
        public int StartBar { get; init; }
        public int BeatPerBar { get; init; }
        public int NoteType { get; init; }
        public long TicksPerBeat { get; init; }
        public long TicksPerBar { get; init; }
    }
}
