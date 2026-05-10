using Microtone.Interfaces.Score.Pitch;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Pitch
{
    internal class AbsolutePitch(float frequency) :  IResolvablePitch, IPitch
    {
        public float Frequency = frequency;
        public double ResolveFrequency(ScoreVariables v) => Frequency;
    }
}
