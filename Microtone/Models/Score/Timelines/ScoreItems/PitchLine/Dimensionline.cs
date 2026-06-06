using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreItems.PitchLine
{
    public class Dimensionline
    {
        public Guid Id { get; internal set; }
        public (Guid, Guid) PitchIds { get; internal set; }
        public DimensionlinePosition _FromPosition = DimensionlinePosition.Left;
        public DimensionlinePosition _ToPosition = DimensionlinePosition.Left;
        public double? offsetX = 0.0;
        public double? offsetLength = 0.0;
        public double? offsetLengthScale = 1.0;
        public AlphaType AlphaType = AlphaType.Normal;
    }
}
