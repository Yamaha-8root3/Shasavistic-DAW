using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    internal class SKHorizontalLineCommand : SKRenderCommand
    {
        public required float Y { get; init; }
        public float? LeftX { get; init; } = null;
        public float? RightX { get; init; } = null;
        public required SKPaint Stroke { get; init; }
        public bool Repeat { get; init; } = false;
        public float RepeatInterval { get; init; } = 0;
        public bool IsOriented { get; init; } = false;
        public bool IgnoreOrigin { get; init; } = false;

        public override SKRect? GetBounds()
        {
            if (LeftX == null || RightX == null) return null;
            var halfH = (Stroke.StrokeWidth / 2f);
            return new SKRect(LeftX.Value, Y - halfH, RightX.Value, Y + halfH);
        }

        public override HitInfo? Render(SKCanvas canvas)
        {
            var X1 = LeftX ?? canvas.LocalClipBounds.Left;
            var X2 = RightX ?? canvas.LocalClipBounds.Right;
            if (Repeat && RepeatInterval != 0)
            {
                if (RepeatInterval > 0)
                {
                    var _yindex = (float)(Y + Math.Floor((canvas.LocalClipBounds.Top - Y) / RepeatInterval));
                    if (IsOriented) _yindex = Math.Max(0, _yindex);
                    // Draw lines until we exceed the right canvas bound
                    for (float yIndex = _yindex; yIndex * RepeatInterval <= canvas.LocalClipBounds.Bottom; yIndex++)
                    {
                        if (IgnoreOrigin && yIndex == 0) continue;
                        var y = yIndex * RepeatInterval;
                        canvas.DrawLine(X1, y, X2, y, Stroke);
                    }
                }
                else
                {
                    var _yindex = (float)(Y + Math.Floor((canvas.LocalClipBounds.Bottom - Y) / RepeatInterval));
                    if (IsOriented) _yindex = Math.Max(0, _yindex);
                    // Draw lines until we exceed the right canvas bound
                    for (float yIndex = _yindex; yIndex * RepeatInterval >= canvas.LocalClipBounds.Top; yIndex++)
                    {
                        if (IgnoreOrigin && yIndex == 0) continue;
                        var y = yIndex * RepeatInterval;
                        canvas.DrawLine(X1, y, X2, y, Stroke);
                    }
                }
                    
            }
            else
            {
                canvas.DrawLine(X1, Y, X2, Y, Stroke);
            }
            return null;
        }
    }
}
