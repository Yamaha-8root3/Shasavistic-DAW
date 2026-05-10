using Microtone.Interfaces.Score;
using Microtone.Models.Score.Timelines.Caches;
using Microtone.Models.Score.Timelines.ScoreVariableItems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines
{
    internal class TimeSignatureMap : ScoreTimeLine<TimeSignatureModifier>, IScoreTimeline
    {
        //private IReadOnlyList<TimeSignatureModifier> Ranges => Items;
        public int PPQ { get; } = 480;

        public double TickToSeconds(long tick)
        {
            return tick;
        }


        
    }
}
