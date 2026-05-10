
using Microtone.Models.Enums;
using Microtone.Models.Score;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering
{
    sealed class PaintFactory(ScoreRenderTheme theme) : IDisposable
    {
        readonly ScoreRenderTheme Theme = theme;
        readonly Dictionary<string, SKPaint> _cache = [];

        public SKPaint Background()
          => Get($"Background", () => new SKPaint
          {
              Color = Theme.Background
          });

        public SKPaint BenchMarkLine(BenchmarkKind kind)
          => Get($"BenchMarkLine-{kind}", () => new SKPaint
          {
              Color = Theme.BenchMarkLineColor[(int)kind],
              StrokeWidth = Theme.BenchMarkLineWidth[(int)kind],
              IsAntialias = true,
              FilterQuality = SKFilterQuality.None,
          });

        public SKPaint Splitter()
          => Get($"Splitter", () => new SKPaint
          {
              Color = Theme.SplitterColor,
              StrokeWidth = Theme.SplitterWidth,
              IsAntialias = true,
              FilterQuality = SKFilterQuality.None,
          });

        public SKPaint Scoreline(int dimension)
          => Get($"{dimension}D-Scoreline", () => new SKPaint
          {
              Color = Theme.DimensionColor[dimension].WithAlpha(Theme.ScoreLineAlpha[dimension]),
              StrokeWidth = Theme.ScorelineWidth[dimension],
              IsAntialias = true,
              FilterQuality = SKFilterQuality.None
          });

        public SKPaint Pitchline(PitchlineType lineType,AlphaType alphaType,bool isDotted)
          => Get($"PitchLine-{lineType}-{alphaType}-{isDotted}", () => new SKPaint
          {
              Color = Theme.PitchLineColor[(int)lineType].WithAlpha(Theme.PitchLineAlpha[(int)alphaType]),
              StrokeWidth = Theme.Pitchline_Height,
              IsAntialias = true,
              FilterQuality = SKFilterQuality.None,
              PathEffect = isDotted ? SKPathEffect.CreateDash(
                        new float[] { 10, 10 }, // 描画, 空白
                        0                     // 開始オフセット
                    ) : null
          });

        public SKPaint BaseTriangle()
          => Get($"BaseTriangle", () => new SKPaint
          {
              Color = Theme.BaseTriangleColor,
              StrokeWidth = Theme.BaseTriangleStrokeWidth,
              IsAntialias = true,
              FilterQuality = SKFilterQuality.None,
              Style = SKPaintStyle.Stroke,
          });

        public SKPaint DimensionLine(int dimension, AlphaType alphaType)
          => Get($"{dimension}D-DimensionLine-{alphaType}", () => new SKPaint
          {
              Color = Theme.DimensionColor[dimension].WithAlpha(Theme.DimensionLineAlpha[(int)alphaType]),
              StrokeWidth = Theme.DimensionLine_Width,
              IsAntialias = true,
              FilterQuality = SKFilterQuality.None,
          });

        public SKPaint SelectionOverlay() => new SKPaint
        {
            Color = Theme.SelectionColor,
            Style = SKPaintStyle.Fill,
        };
        public SKPaint SelectionBorder() => new SKPaint
        {
            Color = Theme.SelectionBorderColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true,
        };



        SKPaint Get(string key, Func<SKPaint> create)
        {
            if (_cache.TryGetValue(key, out var p)) return p;
            p = create();
            _cache[key] = p;
            return p;
        }

        public void Dispose()
        {
            foreach (var p in _cache.Values)
                p.Dispose();
            _cache.Clear();
        }
    }
}
