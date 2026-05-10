using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using static Microtone.Services.GridLine;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    public class SKGridCommand : SKRenderCommand
    {
        public required float StartX { get; init; }
        public required float EndX { get; init; }
        public required float SectionStartX { get; init; }
        public required float FinePixelInterval { get; init; }
        public required GridLineColors Colors { get; init; }
        public required float StrokeWidth { get; init; }

        public override SKRect? GetBounds() => null;

        public override HitInfo? Render(SKCanvas canvas)
        {
            var clip = canvas.LocalClipBounds;
            float drawLeft = Math.Max(StartX, clip.Left);
            float drawRight = Math.Min(EndX, clip.Right);
            if (drawLeft >= drawRight) return null;

            float top = clip.Top;
            float bottom = clip.Bottom;

            long firstStep = (long)Math.Ceiling((drawLeft - SectionStartX) / FinePixelInterval);
            long lastStep = (long)Math.Floor((drawRight - SectionStartX) / FinePixelInterval);

            using var paint = new SKPaint
            {
                StrokeWidth = StrokeWidth,
                IsAntialias = true,
            };

            for (long step = firstStep; step <= lastStep; step++)
            {
                float x = SectionStartX + step * FinePixelInterval;

                // Levels は StepDivisor 降順（粗い順）なので最初に割り切れたものが最優先
                SKColor color = Colors.Fallback;
                foreach (var level in Colors.Levels)
                {
                    if (step % level.StepDivisor == 0) { color = level.Color; break; }
                }

                paint.Color = color;
                canvas.DrawLine(x, top, x, bottom, paint);
            }

            return null;
        }
    }
}
