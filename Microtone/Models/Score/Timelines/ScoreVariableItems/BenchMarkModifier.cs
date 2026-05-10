using Microtone.Interfaces.Score;
using Microtone.Interfaces.Score.Pitch;
using Microtone.Models.Enums;
using Microtone.Models.Score.Pitch;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreVariableItems
{
    internal class BenchMarkModifier : ITimelinePoint
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long StartTick { get; set; } = 0;
        public double? TimeSeconds { get; set; } = null;
        public void UpdateTime(double timeSeconds)
        {
            TimeSeconds = timeSeconds;
        }
        public IPitch? Resolutive { get; set; } = null;
        public IPitch? Modal { get; set; } = null;
        public BenchMarkModifier(long start, IPitch? resolutive, IPitch? modal)
        {
            StartTick = start;
            Resolutive = resolutive;
            Modal = modal;
        }
        public BenchMarkModifier(long start) : this(start, null, null) { }
    }
}
