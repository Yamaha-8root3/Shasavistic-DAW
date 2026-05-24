using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Microtone.Interfaces.Editor;
using Microtone.Models;
using Microtone.Models.Enums;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.HitTest;
using Microtone.Models.Rendering.SKRenderCommands;
using Microtone.Models.Score;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using Microtone.Services.Editor.Selection;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microtone.Services
{
  internal class ChordDiagramFactory
  {
    public static List<SKRenderCommand> BuildChordDiagramWithLayout(ChordDiagram cd, ScoreVariables v, ScoreRenderTheme theme, PaintFactory paint, ISelectionState? selectionState = null, long? overrideTick = null)
    {
      List<SKRenderCommand> commands = [];

      var cdselected = selectionState is ChordDiagramSelection;
      var selectedPitchlineIds = (selectionState as PitchlineSelection)?.Ids ?? [];

      var baseX = (float)(overrideTick ?? cd.StartTick) / v.PPQ * v.PixelPerQuarter;
      var layout = cd.ToLayout(v, theme);

      foreach (var pl in layout.Pitchlines)
      {
        var plY = (float)(-Math.Log2(cd.RootFormula.ResolveFrequency(v) * pl.Source.Formula.RatioValue / v.Initial) * v.Spacing_1D);
        var plX1 = baseX + (float)(pl.Span.Start.Scale * theme.Pitchline_Width + pl.Span.Start.Offset);
        var plX2 = baseX + (float)(pl.Span.End.Scale * theme.Pitchline_Width + pl.Span.End.Offset);


        if (pl.BaseTrianglePosition != PitchlineSymbolPosition.None)
        {
          var triX = pl.BaseTrianglePosition == PitchlineSymbolPosition.Left
              ? plX1 - 15
              : plX2 + 15;
          var angleDeg = pl.BaseTrianglePosition == PitchlineSymbolPosition.Left ? 0f : 180f;
          commands.Add(new SKBaseTriangleCommand
          {
            ZIndex = 25,
            X = triX,
            Y = plY,
            Size = 10,
            AngleDegrees = angleDeg,
            Stroke = paint.BaseTriangle(),
            SourceId = cd.Id,
            PitchlineId = pl.Source.Id,
          });
        }

        //線描画時にDimensionlineとの干渉を防ぐために縮める 縮める前に行いたい処理は上で行う
        if (pl.Span.ShrinkLeft) plX1 += theme.DimensionLine_Width;
        if (pl.Span.ShrinkRight) plX2 -= theme.DimensionLine_Width;

        commands.Add(new SKHorizontalLineCommand
        {
          ZIndex = 20,
          LeftX = plX1,
          RightX = plX2,
          Y = plY,
          Stroke = paint.Pitchline(pl.Source.Type, pl.Source.AlphaType, pl.IsDotted),
        });

        //Pitchline当たり判定
        var pitchlineBounds = new SKRect(plX1, plY - theme.Pitchline_Height / 2,
                                  plX2, plY + theme.Pitchline_Height / 2);
        pitchlineBounds.Inflate(5, 5);
        commands.Add(new SKHitAreaCommand
        {
          ZIndex = 21,
          Rect = pitchlineBounds,
          HitInfo = new HitInfo
          {
            Kind = HitKind.Pitchline,
            TargetId = cd.Id,
            ZIndex = 21,
            Bounds = pitchlineBounds,
            Attributes = { ["PitchlineId"] = pl.Source.Id }
          }
        });
        //Pitchline選択ハイライト
        if (selectedPitchlineIds.Contains(pl.Source.Id))
        {
          commands.Add(new SKRectCommand
          {
            ZIndex = 20,
            Rect = pitchlineBounds,
            Fill = paint.SelectionOverlay(),
            Stroke = paint.SelectionBorder(),
            CornerRadius = 20
          });
        }

      }
      foreach (var dl in layout.Dimensionlines)
      {
        var lowerY = (float)(-Math.Log2(cd.RootFormula.ResolveFrequency(v) * dl.Lower.Source.Formula.RatioValue / v.Initial) * v.Spacing_1D);
        var higherY = (float)(-Math.Log2(cd.RootFormula.ResolveFrequency(v) * dl.Higher.Source.Formula.RatioValue / v.Initial) * v.Spacing_1D);

        // LowerAnchor / HigherAnchor の T値からX座標を解決
        float LowerX(DimensionlineRenderAnchor anchor)
        {
          var spanStart = baseX + (float)(dl.Lower.Span.Start.Scale * theme.Pitchline_Width + dl.Lower.Span.Start.Offset);
          var spanEnd = baseX + (float)(dl.Lower.Span.End.Scale * theme.Pitchline_Width + dl.Lower.Span.End.Offset);
          return spanStart + (float)(anchor.T * (spanEnd - spanStart)) + (float)anchor.Offset;
        }
        float HigherX(DimensionlineRenderAnchor anchor)
        {
          var spanStart = baseX + (float)(dl.Higher.Span.Start.Scale * theme.Pitchline_Width + dl.Higher.Span.Start.Offset);
          var spanEnd = baseX + (float)(dl.Higher.Span.End.Scale * theme.Pitchline_Width + dl.Higher.Span.End.Offset);
          return spanStart + (float)(anchor.T * (spanEnd - spanStart)) + (float)anchor.Offset;
        }
        float DimensionLineHalfWidth(double t)
        {
          if (t < 0.5 - 1e-6) return theme.DimensionLine_Width / 2; // 左端寄り → 左に広げる
          if (t > 0.5 + 1e-6) return -theme.DimensionLine_Width / 2; // 右端寄り → 右に広げる
          return 0f;                                                   // 中央 → 補正なし
        }

        var lowerOffset = DimensionLineHalfWidth(dl.LowerAnchor.T);
        var higherOffset = DimensionLineHalfWidth(dl.HigherAnchor.T);

        // T値が同じ（左同士・右同士・中央同士）→ SKRectCommand
        if (Math.Abs(dl.LowerAnchor.T - dl.HigherAnchor.T) < 1e-6)
        {
          //起点は長方形の左下
          var fromX = LowerX(dl.LowerAnchor) - theme.DimensionLine_Width / 2;
          var toX = HigherX(dl.HigherAnchor) - theme.DimensionLine_Width / 2;
          commands.Add(new SKRectCommand
          {
            ZIndex = 30 + dl.Dimension,
            Rect = new SKRect(
                  left: fromX + lowerOffset,
                  top: higherY - theme.Pitchline_Height / 2,
                  right: toX + higherOffset + theme.DimensionLine_Width,
                  bottom: lowerY + theme.Pitchline_Height / 2
              ),
            Fill = paint.DimensionLine(dl.Dimension, dl.AlphaType),
          });
        }
        else
        {
          // T値が異なる（斜め接続）→ SKParallelogramCommand
          // 起点は中央
          var fromX = LowerX(dl.LowerAnchor);
          var toX = HigherX(dl.HigherAnchor);
          commands.Add(new SKParallelogramCommand
          {
            ZIndex = 30 + dl.Dimension,
            From = new SKPoint(fromX + lowerOffset, lowerY + theme.Pitchline_Height / 2),
            To = new SKPoint(toX + higherOffset, higherY - theme.Pitchline_Height / 2),
            Width = theme.DimensionLine_Width,
            Fill = paint.DimensionLine(dl.Dimension, dl.AlphaType),
          });
        }
      }

      var bounds = CalcBoundingBox(commands);
      if (bounds != null)
      {
        var expandedBounds = ((SKRect)bounds);
        expandedBounds.Inflate(20, 20);
        commands.Add(new SKHitAreaCommand
        {
          ZIndex = 10, // Pitchline(20)より手前でよい
          Rect = (SKRect)expandedBounds,
          HitInfo = new HitInfo
          {
            Kind = HitKind.ChordDiagramBody,
            TargetId = cd.Id,
            ZIndex = 10,
            Bounds = (SKRect)expandedBounds,
          }
        });
        if (cdselected)
        {
          commands.Add(new SKRectCommand
          {
            ZIndex = 6,
            Rect = expandedBounds,
            Fill = paint.SelectionOverlay(),
            Stroke = paint.SelectionBorder(),
            CornerRadius = 20
          });
        }
      }


      return commands;
    }

    private static SKRect? CalcBoundingBox(IEnumerable<SKRenderCommand> commands)
    {
      float minX = float.MaxValue, minY = float.MaxValue;
      float maxX = float.MinValue, maxY = float.MinValue;
      bool any = false;

      foreach (var cmd in commands)
      {
        var b = cmd.GetBounds();
        if (b == null) continue;
        minX = MathF.Min(minX, b.Value.Left);
        minY = MathF.Min(minY, b.Value.Top);
        maxX = MathF.Max(maxX, b.Value.Right);
        maxY = MathF.Max(maxY, b.Value.Bottom);
        any = true;
      }

      return any ? new SKRect(minX, minY, maxX, maxY) : null;
    }
  }
}
