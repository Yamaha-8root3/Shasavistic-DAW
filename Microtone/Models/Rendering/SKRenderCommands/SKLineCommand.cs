using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    internal class SKLineCommand : SKRenderCommand
    {
        public required SKPoint P0 { get; init; }
        public required SKPoint P1 { get; init; }
        public required SKPaint Stroke { get; init; }

        public override SKRect? GetBounds()
        {
            return new SKRect(
                Math.Min(P0.X, P1.X),
                Math.Min(P0.Y, P1.Y),
                Math.Max(P0.X, P1.X),
                Math.Max(P0.Y, P1.Y)
            );
        }

        public override HitInfo? Render(SKCanvas canvas)
        {
            var bounds = SKRect.Create(
              Math.Min(P0.X, P1.X),
              Math.Min(P0.Y, P1.Y),
              Math.Abs(P0.X - P1.X),
              Math.Abs(P0.Y - P1.Y)
            );
            if (!bounds.IntersectsWith(canvas.LocalClipBounds)) return null ;
            canvas.DrawLine(P0,P1,Stroke);
            return null;
        }
    }
}
