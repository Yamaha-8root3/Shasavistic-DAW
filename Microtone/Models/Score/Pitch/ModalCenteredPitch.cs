using Microtone.Interfaces.Score.Pitch;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Pitch
{
    internal class  ModalCenteredPitch(Harmonograph harmonograph) :  IResolvablePitch, IPitch
    {
        public Harmonograph Harmonograph { get; set; } = harmonograph;
        public double ResolveFrequency(ScoreVariables v)
        {
            if (v.Modal is null) return 0;
            return v.Modal.ResolveFrequency(v) * Harmonograph.ToOvertoneFormula(v.Dimension1DOffset).RatioValue;
        }
    }
}
