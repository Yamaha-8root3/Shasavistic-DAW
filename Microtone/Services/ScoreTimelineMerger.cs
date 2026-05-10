using Microtone.Interfaces.Score;
using Microtone.Models.Score;
using Microtone.Models.Score.Timelines.ScoreVariableItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microtone.Services
{
    internal sealed class ScoreTimelineMerger
    {
        public static IEnumerable<ITimelinePoint> Merge(ScoreData score)
        {
            var enumerators = new List<IEnumerator<ITimelinePoint>>
            {
                score.TimeSignatureMap.Items.GetEnumerator(),
                score.BenchMarkMap.Items.GetEnumerator(),
                score.ScorelineMap.Items.GetEnumerator(),
                score.DefiningChordMap.Items.GetEnumerator(),
            };
            foreach (var tl in score.ScoreTimeLines)
                enumerators.Add(tl.Items.GetEnumerator());

            // 初期 MoveNext
            enumerators.RemoveAll(e => !e.MoveNext());

            while (enumerators.Count > 0)
            {
                var next = enumerators
                    .MinBy(e => (e.Current.StartTick, GetPriority(e.Current)));

                if (next == null) break;
                yield return next.Current;

                if (!next.MoveNext())
                    enumerators.Remove(next);
            }
        }
        private static int GetPriority(ITimelinePoint item)
        {
            return item switch
            {
                TimeSignatureModifier => 0,  // 最優先
                BenchMarkModifier => 1,
                ScorelineModifier => 2,
                DefiningChord => 3,
                _ => 999
            };
        }
    }

}
