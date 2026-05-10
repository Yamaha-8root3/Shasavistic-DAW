using Microtone.Interfaces.Score.Pitch;
using Microtone.Models;
using Microtone.Models.Score;
using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services
{
    internal class PitchCalculator
    {
        public static IResolvablePitch? CalcNextPitch(ScoreVariables v, IPitch? pitch)
        {
            if (pitch == null) return null;
            if (pitch is IResolvablePitch) return (IResolvablePitch?)pitch;
            //if (target)
            return null;
        }

        //public static Dimensions<short> HarmonographToDimensions(OvertoneFormula harmonograph, Dimensions<OvertoneFormula> difinision)
        //{
        //    var d = new Dimensions<short>();
        //    for (int i = harmonograph.MaxDimension; i >= 2; i--)
        //    {
        //        while (harmonograph[i] > 0)
        //        {
        //            harmonograph -= difinision[i];
        //            d[i] += 1;
        //        }
        //    }
        //    d[1] = harmonograph[1];
        //    return d;
        //}
    }
}
