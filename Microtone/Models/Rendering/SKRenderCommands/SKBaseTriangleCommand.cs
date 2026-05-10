using Microtone.Interfaces.Score;
using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    internal class SKBaseTriangleCommand : SKRenderCommand
    {
        public required float X { get; init; }
        public required float Y { get; init; }
        public required float Size { get; init; }
        public required float AngleDegrees { get; init; }
        public required SKPaint Stroke { get; init; }
        public required Guid SourceId { get; init; }
        public required int PitchlineId { get; init; }
        public override HitInfo? Render(SKCanvas canvas)
        {
            var path = new SKPath();
            float angleRad = AngleDegrees * (float)Math.PI / 180;

            for (int i = 0; i < 3; i++)
            {
                float angle = angleRad + i * 2 * (float)Math.PI / 3;
                float x = X + Size * (float)Math.Cos(angle);
                float y = Y + Size * (float)Math.Sin(angle);

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            path.Close();
            if (!canvas.QuickReject(path))
            {
                canvas.DrawPath(path, Stroke);
                var p = Stroke.Clone();
                p.Style = SKPaintStyle.StrokeAndFill;
                return new()
                {
                     TargetId = SourceId,
                     Bounds = path.Bounds,
                     Path = p.GetFillPath(path),
                     ZIndex = ZIndex,
                     Attributes = { ["PitchlineId"] = PitchlineId }
                };
            }
            else
            {
                return null;
            }
        }
        public override SKRect? GetBounds() =>
            new SKRect(X - Size, Y - Size, X + Size, Y + Size);
    }
}
