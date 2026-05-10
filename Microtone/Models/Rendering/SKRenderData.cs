using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering
{
    public class SKRenderData
    {
        public SKPaint Background { get; set; } = new();
        public List<SKRenderCommand> Commands { get; init; } = [];
        public void SortCommandsByZIndex()
        {
            Commands.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
        }
    }
}
