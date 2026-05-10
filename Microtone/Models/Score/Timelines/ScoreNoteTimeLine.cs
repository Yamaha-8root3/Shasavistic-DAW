using Microtone.Interfaces.Score;
using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microtone.Models.Score.Timelines
{
    internal class ScoreNoteTimeLine<T> : ScoreTimeLine<T> where T : ITimelineItem

    {
        
        public void ResolveEnd()
        {
            var ids = _idByTick.ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                Guid guid = ids[i].Value;
                var cur = _items[guid];
                if (cur.LengthKind != LengthKind.UntilNext)
                    continue;

                if (i + 1 < ids.Length)
                {
                    cur.LengthTick = _items[ids[i + 1].Value].StartTick - cur.StartTick;
                    cur.Isresolved = true;
                }
                else
                {
                    //cur.LengthTick = long.MaxValue;
                    cur.Isresolved = false;
                }
            }


        }
    }
}
