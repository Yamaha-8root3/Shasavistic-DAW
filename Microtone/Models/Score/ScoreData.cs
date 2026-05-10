using Microtone.Interfaces.Score;
using Microtone.Models.Score.Timelines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score
{
    internal class ScoreData
    {
        public ScoreData(byte timelineCount)
        {
            for (int i = 0; i < timelineCount; i++)
                ScoreTimeLines.Add(new());
        }
        public TimeSignatureMap TimeSignatureMap { get; set; } = new();
        public BenchMarkMap BenchMarkMap { get; set; } = new();
        public ScorelineMap ScorelineMap { get; set; } = new();
        public DefiningChordMap DefiningChordMap { get; set; } = new();
        public List<ScoreNoteTimeLine<ITimelineItem>> ScoreTimeLines { get; set; } = [];
        //public Dimensions<OvertoneFormula> DimensionDefinition { get; set; } = new([
        //    new([1]), // 1D 2
        //    new([-1,1]), // 2D 3/2
        //    new([-2,0,1]), // 3D 5/4
        //    new([-2,0,0,1]), // 4D 7/4
        //    new([-2,0,0,0,1]), // 5D 11/4
        //    new([-3,0,0,0,0,1]), // 6D 13/8
        //    new([-4,0,0,0,0,0,1]) // 7D 17/16
        //    ]);
        public Dimensions<int> Dimension1DOffset { get; set; } = new(
            [0,-1,-2,-2,-2,-3,-4]);
    }
}
