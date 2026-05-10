using Microtone.Interfaces.Score;
using Microtone.Models.Score.Timelines.ScoreVariableItems;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microtone.Models.Score.Timelines
{
    internal class BenchMarkMap : ScoreTimeLine<BenchMarkModifier>, IScoreTimeline
    {
        /// <summary>
        /// 拠音の周波数(hz)
        /// default = 261.63Hz
        /// </summary>
        public double Initial { get; set; } = 261.63;
    }
}
