using Microtone.Interfaces.Score;
using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering
{
    public abstract class SKRenderCommand
    {
        public int ZIndex { get; init; }
        public ITimelinePoint? SourceItem { get; init; }
        public abstract HitInfo? Render(SKCanvas canvas);
        public abstract SKRect? GetBounds();
        protected static SKRect GetClipBounds(SKRect bounds,SKRect clipBounds)
        {
            return new SKRect
            {
                Left = Math.Max(bounds.Left, clipBounds.Left),
                Top = Math.Max(bounds.Top, clipBounds.Top),
                Right = Math.Min(bounds.Right, clipBounds.Right),
                Bottom = Math.Min(bounds.Bottom, clipBounds.Bottom)
            };
        }
        protected static SKPoint GetClipPoint(SKPoint point, SKRect clipBounds)
        {
            return new SKPoint
            {
                //X = Math.Min(Math.Max(bounds.X, clipBounds.Left),clipBounds.Right),
                //Y = Math.Min(Math.Max(bounds.Y, clipBounds.Top),clipBounds.Bottom)
                X = Math.Clamp(point.X,clipBounds.Left,clipBounds.Right),
                Y = Math.Clamp(point.Y, clipBounds.Top, clipBounds.Bottom)
            };
        }
    }
}
