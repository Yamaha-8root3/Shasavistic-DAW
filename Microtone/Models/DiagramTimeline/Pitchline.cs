
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using static Microtone.Models.DiagramTimeline.Rendering.ChartRenderTheme;

namespace Microtone.Models.DiagramTimeline.Rendering
{
    public class Pitchline
    {
        public SKPoint Offset { get; set; } = new(0f, 0f);
        public OvertoneFormula Harmonograph { get; set; } = new([0]);
        public PitchLineType LineType { get; set; } = PitchLineType.Normal;
        public AlphaType PitchAlphaType { get; set; } = AlphaType.Normal;
        public bool IsDotted { get; set; } = false;
        public bool IsBase { get; set; } = false;
        public enum NodeType
        {
            None = 0,
            Right = 1,
            Left = 2,
        }
        public NodeType HasNode { get; set; } = NodeType.None;
        public float Widthscale { get; set; } = 1f;
        public float WidthOffset { get; set; } = 0f;
        public List<DimensionLine> DimensionLines { get; set; } = [];
        public List<Pitchline> PitchLines { get; set; } = [];
    }
}
