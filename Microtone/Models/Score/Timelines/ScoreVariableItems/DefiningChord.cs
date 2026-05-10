using Microtone.Interfaces.Score;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreVariableItems
{
    public class DefiningChord : ITimelinePoint
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long StartTick { get; set; } = 0;
        public double? TimeSeconds { get; set; } = null;
        public void UpdateTime(double timeSeconds)
        {
            TimeSeconds = timeSeconds;
        }

        public Harmonograph? Host { get; set; } = null;
        public Harmonograph? Guest { get; set; } = null;

        public DefiningChord() { }
        public DefiningChord(Harmonograph host, Harmonograph guest)
        {
            Host = host; 
            Guest = guest;
        }
    }
}
