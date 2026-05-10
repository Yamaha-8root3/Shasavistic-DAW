using Microtone.Interfaces.Score.Pitch;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microtone.Models.Score.Pitch
{
    internal class InitialCenteredPitch(Harmonograph harmonograph) : IResolvablePitch, IPitch
    {
        public Harmonograph Harmonograph { get; set; } = harmonograph;
        public double ResolveFrequency(ScoreVariables v) => v.Initial * Harmonograph.ToOvertoneFormula(v.Dimension1DOffset).RatioValue;
    }
}
