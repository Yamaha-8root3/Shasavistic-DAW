using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services
{
    internal static class GridSnapper
    {
        public static long SnapTick(long tick, TimeSignatureMap timeSignatureMap, int division, int? unsnapDistance = null)
        {
            int ppq = timeSignatureMap.PPQ;
            long gridTick = ppq * 4L / division;


            var origin = timeSignatureMap.QueryLatestByTick(tick);
            if (origin == null) return tick;

            var delta = tick - origin.StartTick;
            var snappedDelta = (long)Math.Round((decimal)delta / (decimal)gridTick) * gridTick;

            if (unsnapDistance != null && Math.Abs(delta - snappedDelta) > unsnapDistance) return tick;

            return origin.StartTick + snappedDelta;
        }
    }
}
