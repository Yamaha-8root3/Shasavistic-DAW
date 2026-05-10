using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    internal class SKRectCommand : SKRenderCommand
    {
        public required SKRect Rect { get; init; }
        public SKPaint? Fill { get; init; } = null;
        public SKPaint? Stroke { get; init; } = null;
        public float CornerRadius { get; init; } = 0;


        public override SKRect? GetBounds() => Rect;
        public override HitInfo? Render(SKCanvas canvas)
        {
            SKRect _rect = GetClipBounds(Rect, canvas.LocalClipBounds);
            if (_rect.Width <= 0 || _rect.Height <= 0) return null;
            if (Fill != null)
            {
                if (CornerRadius > 0) {
                    canvas.DrawRoundRect(Rect, CornerRadius, CornerRadius, Fill);
                }
                else
                {
                    canvas.DrawRect(Rect, Fill);
                }
            }

            if (Stroke != null)
            {
                if (CornerRadius > 0) {
                    canvas.DrawRoundRect(Rect, CornerRadius, CornerRadius, Stroke);
                }
                else
                {
                    canvas.DrawRect(Rect, Stroke);
                }
            }
            return null;
        }
    }
}
