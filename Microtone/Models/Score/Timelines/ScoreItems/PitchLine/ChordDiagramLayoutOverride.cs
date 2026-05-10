using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreItems.PitchLine
{
    /// <summary>
    /// Pitchline の見た目オーバーライド。音高には影響しない。
    /// null = オーバーライドなし（デフォルトルールを使用）。
    /// </summary>
    public class PitchlineOverride
    {
        public PitchlineRenderSpan? Span { get; set; } = null;
        public PitchlineSymbolPosition? NodePosition { get; set; } = null;
        public PitchlineSymbolPosition? BaseTrianglePosition { get; set; } = null;
        public AlphaType? AlphaType { get; set; } = null;
        public PitchlineType? Type { get; set; } = null;
    }

    public class DimensionlineOverride { 
        public DimensionlineRenderAnchor? LowerAnchor { get; set; } = null;
        public DimensionlineRenderAnchor? HigherAnchor { get; set; } = null;
        public AlphaType? AlpType { get; set; } = null;
    }
}
