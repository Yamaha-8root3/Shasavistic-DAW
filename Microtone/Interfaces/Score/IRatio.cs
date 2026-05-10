using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Interfaces.Score
{
    internal interface IRatio
    {
        int Numerator { get; }
        int Denominator { get; }
        public double Value => (double)Numerator / Denominator;
    }
}
