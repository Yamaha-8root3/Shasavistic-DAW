using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    internal class SKParallelogramCommand : SKRenderCommand
    {
        public SKPoint From { get; init; }
        public SKPoint To { get; init; }
        public float Width { get; init; }
        public SKPaint? Fill { get; init; } = null;
        //public SKPaint? Stroke { get; init; } = null;


        public override HitInfo? Render(SKCanvas canvas)
        {
            using var path = new SKPath();
            path.MoveTo(From.X - Width / 2, From.Y);
            path.LineTo(To.X - Width / 2, To.Y);
            path.LineTo(To.X + Width / 2, To.Y);
            path.LineTo(From.X + Width / 2, From.Y);
            path.Close();

            canvas.DrawPath(path, Fill);
            return null;
        }
        public override SKRect? GetBounds()
        {
            using var path = new SKPath();
            path.MoveTo(From.X - Width / 2, From.Y);
            path.LineTo(To.X - Width / 2, To.Y);
            path.LineTo(To.X + Width / 2, To.Y);
            path.LineTo(From.X + Width / 2, From.Y);
            path.Close();
            return path.Bounds;
        }

    }
}
