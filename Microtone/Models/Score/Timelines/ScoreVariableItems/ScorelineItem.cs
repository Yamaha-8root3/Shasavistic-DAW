using Microtone.Interfaces.Score;
using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreVariableItems
{
    public struct ScorelineItem : ITimelinePoint
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long StartTick { get; set; } = 0;
        public double? TimeSeconds { get; set; } = null;
        public void UpdateTime(double timeSeconds)
        {
            TimeSeconds = timeSeconds;
        }
        public OvertoneFormula Interval { get; set; } = new([0]);
        public OvertoneFormula Offset { get; set; } = new([]);
        public bool Infinity { get; set; } = true;
        public bool IsOriented { get; set; } = false;
        public bool IgnoreOrigin { get; set; } = false;

        public ScorelineItem(OvertoneFormula offset, bool infinity, OvertoneFormula interval ,bool isoriented, bool ignoreOrigin) {
            Interval = interval;
            Offset = offset;
            Infinity = infinity;
            IsOriented = isoriented;
            IgnoreOrigin = ignoreOrigin;
        }
        public ScorelineItem(bool infinity, OvertoneFormula interval, bool isoriented, bool ignoreOrigin) : this(new([0]), infinity, interval, isoriented, ignoreOrigin) { }
        public ScorelineItem(OvertoneFormula offset) : this(offset, false, new([0]), false, false) { }
    }
}
