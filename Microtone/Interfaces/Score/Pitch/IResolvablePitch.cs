using Microtone.Models.Score;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Interfaces.Score.Pitch
{
    public interface IResolvablePitch : IPitch
    {
        public double ResolveFrequency(ScoreVariables v);
    }
}
