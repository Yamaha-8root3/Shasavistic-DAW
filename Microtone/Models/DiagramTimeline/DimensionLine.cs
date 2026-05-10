using System;
using System.Collections.Generic;
using System.Text;
using static Microtone.Models.DiagramTimeline.Rendering.ChartRenderTheme;

namespace Microtone.Models.DiagramTimeline.Rendering
{
    public class DimensionLine(byte dimension, short scending, AlphaType alpha)
    {
        public byte Dimension = dimension;
        public short Scending = scending;
        public AlphaType alpha = alpha;

        public DimensionLine(byte dimension, short scending): this(dimension, scending, AlphaType.Normal) { }
    }
}
