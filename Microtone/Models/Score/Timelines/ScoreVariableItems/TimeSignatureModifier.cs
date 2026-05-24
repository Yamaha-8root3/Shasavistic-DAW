using Microtone.Interfaces.Score;
using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreVariableItems
{
    public class TimeSignatureModifier : ITimelinePoint
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long StartTick { get; set; } = 0;
        public double? TimeSeconds { get; set; } = null;
        public void UpdateTime(double timeSeconds)
        {
            TimeSeconds = timeSeconds;
        }
        public double? Bpm { get; } = null;
        public int? BeatPerBar { get; }
        public int? BeatType { get; }

        public TimeSignatureModifier(long start, double bpm, int beatPerBar, int noteType)
        {
            StartTick = start;
            Bpm = bpm;
            BeatPerBar = beatPerBar;
            BeatType = noteType;
        }
    }
}
