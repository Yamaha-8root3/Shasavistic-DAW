using Microtone.Interfaces.Score;
using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score
{
    internal abstract class TimelineItemTemp : ITimelinePoint
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long StartTick { get; set; } = 0;
        public long LengthTick { get; set; } = 0;
        public LengthKind LengthKind { get; set; } = LengthKind.Fixed;
        public double? TimeSeconds { get; set; } = null;
        public void UpdateTime(double timeSeconds)
        {
            TimeSeconds = timeSeconds;
        }

        //public TimelineItem(MusicalTick start, MusicalTick length, LengthKind lengthKind)
        //{
        //    Start = start;
        //    Length = length;
        //    LengthKind = lengthKind;
        //}

        //public TimelineItem(MusicalTick start, MusicalTick length) : this(start, length, LengthKind.Fixed) { }
        //public TimelineItem(MusicalTick start) : this(start, new(), LengthKind.Infinite) { }

    }
}
