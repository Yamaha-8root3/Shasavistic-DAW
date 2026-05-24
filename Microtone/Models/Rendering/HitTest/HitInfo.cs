using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering.HitTest
{
    public class HitInfo
    {
        public HitKind Kind { get; set; } = HitKind.None;
        public Guid TargetId { get; set; }
        public SKRect Bounds { get; set; }
        public SKPath? Path { get; set; } = null;
        public int ZIndex { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = [];
    }

    public enum HitKind
    {
        None,
        ChordDiagramBody,
        Pitchline,
  }
}
