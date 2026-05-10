using Microtone.Interfaces.Score;
using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreVariableItems
{
    internal class ScorelineModifier : ITimelinePoint
    {
        public Guid Id { get; } = Guid.NewGuid();

        public long StartTick { get; set; } = 0;

        public double? TimeSeconds { get; set; } = null;
        public ModifyType ModifyType { get; set; } = ModifyType.Set;

        public List<ScorelineItem> Lines { get; set; } = [];

        public void UpdateTime(double timeseconds)
        {
            TimeSeconds = timeseconds;
        }

        public ScorelineModifier(long start, IEnumerable<ScorelineItem> lines)
        {
            StartTick = start;
            // IEnumerable<ScorelineItem> から List<ScorelineItem> に変換してから StartTick を設定
            var lineList = lines.ToList();
            for (int i = 0; i < lineList.Count; i++)
            {
                var item = lineList[i];
                item.StartTick = start;
                lineList[i] = item;
            }
            Lines = lineList;
        }
    }
}
