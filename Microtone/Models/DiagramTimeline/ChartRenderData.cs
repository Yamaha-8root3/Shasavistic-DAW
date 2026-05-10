using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.DiagramTimeline.Rendering
{
    public sealed class ChartRenderData
    {
        public event Action? Changed;
        public Timeline<Dimensions<bool>> DimensionLine { get; init; } = new();
        public Timeline<Pitchline> Pitchlines { get; init; } = new();
        public Dimensions<float> DimensionRatio { get; init; } = new([
            0,
            MathF.Log2(2/1),     // 1D
            MathF.Log2(3f/2f),   // 2D
            MathF.Log2(5f/4f),   // 3D
            MathF.Log2(7f/4f),   // 4D
            MathF.Log2(11f/4f),  // 5D
            MathF.Log2(13f/8f),  // 6D
            MathF.Log2(17f/16f), // 7D
        ], 0);
    }
}
