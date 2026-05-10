using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    internal class SKVerticalLineCommand : SKRenderCommand
    {
        public required float X { get; init; }
        public float? TopY { get; init; } = null;
        public float? BottomY { get; init; } = null;
        public required SKPaint Stroke { get; init; }
        public bool Repeat { get; init; } = false;
        public float RepeatInterval { get; init; } = 0;
        public bool IsOriented { get; init; } = false;
        public bool IgnoreOrigin { get; init; } = false;

        public override SKRect? GetBounds() => null;
        public override HitInfo? Render(SKCanvas canvas)
        {
            var Y1 = TopY ?? canvas.LocalClipBounds.Top;
            var Y2 = BottomY ?? canvas.LocalClipBounds.Bottom;
            if (Repeat && RepeatInterval != 0)
            {
                //// Adjust startX to the leftmost position within the canvas bounds
                //var _x = (float)(X + Math.Floor((canvas.LocalClipBounds.Right - X) / RepeatInterval) * RepeatInterval);
                //// Draw lines until we exceed the right canvas bound
                //for (float currentX = _x; currentX >= canvas.LocalClipBounds.Left; currentX -= RepeatInterval)
                //{
                //    canvas.DrawLine(Y1, currentX, Y2, currentX, Stroke);
                //}

                if (RepeatInterval > 0)
                {
                    var _xindex = (float)(X + Math.Floor((canvas.LocalClipBounds.Left - X) / RepeatInterval));
                    if (IsOriented) _xindex = Math.Max(0, _xindex);
                    // Draw lines until we exceed the right canvas bound
                   for (float xIndex = _xindex; xIndex * RepeatInterval <= canvas.LocalClipBounds.Right; xIndex++)
                    {
                        if (IgnoreOrigin && xIndex == 0) continue;
                        var x = xIndex * RepeatInterval;
                        canvas.DrawLine(x,Y1,x,Y2, Stroke);
                    }
                }
                else
                {
                    var _xindex = (float)(X + Math.Floor((canvas.LocalClipBounds.Right - X) / RepeatInterval));
                    if (IsOriented) _xindex = Math.Max(0, _xindex);
                    // Draw lines until we exceed the right canvas bound
                    for (float xIndex = _xindex; xIndex * RepeatInterval >= canvas.LocalClipBounds.Left; xIndex++)
                    {
                        if (IgnoreOrigin && xIndex == 0) continue;
                        var x = xIndex * RepeatInterval;
                        canvas.DrawLine(x,Y1,x,Y2, Stroke);
                    }
                }
            }
            else
            {
                canvas.DrawLine(X, Y1, X, Y2, Stroke);
            }

            return null;
        }

    }
}
