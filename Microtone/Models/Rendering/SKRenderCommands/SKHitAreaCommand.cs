using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering.SKRenderCommands
{
    // 描画は何もしないが HitInfo だけ返すコマンド
    internal class SKHitAreaCommand : SKRenderCommand
    {
        public required SKRect Rect { get; init; }
        public required HitInfo HitInfo { get; init; }
        public override SKRect? GetBounds() => null;

        public override HitInfo? Render(SKCanvas canvas)
        {
            HitInfo.Bounds = Rect;
            return HitInfo;
        }
    }
    
}
