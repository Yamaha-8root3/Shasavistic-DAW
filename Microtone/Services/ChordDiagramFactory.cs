using Avalonia.Controls.Primitives;
using Microtone.Models;
using Microtone.Models.Enums;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.HitTest;
using Microtone.Models.Rendering.SKRenderCommands;
using Microtone.Models.Score;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
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
        public static List<SKRenderCommand> BuildChordDiagramWithLayout(ChordDiagram cd, ScoreVariables v, ScoreRenderTheme theme, PaintFactory paint, bool selected = false, long? overrideTick = null)
        {
            List<SKRenderCommand> commands = [];
            
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
            commands.Add(new SKHitAreaCommand
                {
                    ZIndex = 10, // Pitchline(20)より手前でよい
                    Rect = (SKRect)bounds,
                    HitInfo = new HitInfo
                        {
                        Kind = HitKind.ChordDiagramBody,
                        TargetId = cd.Id,
                        ZIndex = 10,
                        Bounds = (SKRect)bounds,
                }
                });
                if (selected)
                {
                    var expandedBounds = ((SKRect)bounds);
                    expandedBounds.Inflate(20, 20);
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


        //未使用 非推奨
        public static List<SKRenderCommand> BuildChordDiagram(ChordDiagram cd, ScoreVariables v, ScoreRenderTheme theme, PaintFactory paint)
        {
            List<SKRenderCommand> commands = [];

            var baseX = (float)cd.StartTick / v.PPQ * v.PixelPerQuarter;
            foreach (var dlk in cd.Dimensionlines)
            {
                var dl = dlk.Value;
                var dlpath = cd.GetDimensionlinePath(dl.Id);

                var from = dlpath.Lower;
                var fromX1 = baseX + (float)(from.offsetXByScale.GetValueOrDefault(0) * theme.Pitchline_Width) + (float)from.offsetX.GetValueOrDefault(0);
                var fromX2 = (float)(fromX1 + from.offsetLengthScale.GetValueOrDefault(1) * theme.Pitchline_Width + from.offsetLength.GetValueOrDefault(0));
                var to = dlpath.Higher;
                var toX1 = baseX + (float)(to.offsetXByScale.GetValueOrDefault(0) * theme.Pitchline_Width) + (float)to.offsetX.GetValueOrDefault(0);
                var toX2 = (float)(toX1 + to.offsetLengthScale.GetValueOrDefault(1) * theme.Pitchline_Width + to.offsetLength.GetValueOrDefault(0));
                var formula = from.Formula;
                var interval = to.Formula - from.Formula;
                var harmonograph = new Harmonograph(interval, v.Dimension1DOffset);

                var MinD = 1;
                while (harmonograph[MinD] == 0 && MinD < harmonograph.MaxDimension)
                {
                    MinD++;
                }

                dl._FromPosition = theme.DimensionLineFromPosition[MinD];
                if (harmonograph[harmonograph.MaxDimension] > 0)
                {
                    dl._ToPosition = theme.DimensionLineToPosition[harmonograph.MaxDimension];
                }
                else
                {
                    dl._ToPosition = theme.DimensionLineFromPosition[harmonograph.MaxDimension];
                }
                for (var d = 2; d <= harmonograph.MaxDimension; d++)
                {
                    if (harmonograph[d] == 0) continue;
                    //var _negative = harmonograph[d] < 0;
                    for (var i = 1; i <= Math.Abs(harmonograph[d]); i++)
                    {
                        SKRect rect = new();
                        //if (_negative)
                        //{
                        //    rect.Top = (float)(-Math.Log2((cd.Frequency * formula.RatioValue) / v.Initial) * theme.Spacing_1D) - theme.Pitchline_Height / 2;
                        //    formula -= score.DimensionDefinition[d];
                        //    rect.Bottom = (float)(-Math.Log2((cd.Frequency * (formula.RatioValue)) / v.Initial) * theme.Spacing_1D) + theme.Pitchline_Height / 2;
                        //}
                        //else
                        //{
                        rect.Bottom = (float)(-Math.Log2((cd.RootFormula.ResolveFrequency(v) * formula.RatioValue) / v.Initial) * v.Spacing_1D) + theme.Pitchline_Height / 2;
                        formula[d] += 1;
                        formula[1] += v.Dimension1DOffset[d]!;
                        rect.Top = (float)(-Math.Log2((cd.RootFormula.ResolveFrequency(v) * (formula.RatioValue)) / v.Initial) * v.Spacing_1D) - theme.Pitchline_Height / 2;
                        //}
                        if (theme.DimensionLineFromPosition[d] == theme.DimensionLineToPosition[d])
                        {
                            switch (theme.DimensionLineFromPosition[d])
                            {
                                case DimensionlinePosition.Left:
                                    rect.Left = fromX1 - theme.DimensionLine_Width;
                                    rect.Right = toX1;
                                    break;
                                case DimensionlinePosition.Right:
                                    rect.Left = fromX2;
                                    rect.Right = toX2 + theme.DimensionLine_Width;
                                    break;
                                case DimensionlinePosition.Center:
                                    rect.Left = (fromX1 + fromX2) / 2 - theme.DimensionLine_Width / 2;
                                    rect.Right = (toX1 + toX2) / 2 + theme.DimensionLine_Width / 2;
                                    break;
                            }
                            commands.Add(new SKRectCommand()
                            {
                                ZIndex = 30 + d,
                                Rect = rect,
                                Fill = paint.DimensionLine(d, dl.AlphaType)
                            });
                        }
                        else
                        {
                            SKPoint _from;
                            SKPoint _to;
                            if (theme.DimensionLineFromPosition[d] < theme.DimensionLineToPosition[d])
                            {
                                _from = new SKPoint(fromX1 - theme.DimensionLine_Width / 2, rect.Bottom);
                                _to = new SKPoint(toX2 + theme.DimensionLine_Width / 2, rect.Top);
                                //if (_negative) (_from, _to) = (_to, _from);
                            }
                            else
                            {
                                _from = new SKPoint(fromX2 + theme.DimensionLine_Width / 2, rect.Bottom);
                                _to = new SKPoint(toX1 - theme.DimensionLine_Width / 2, rect.Top);
                                //if (_negative) (_from, _to) = (_to, _from);
                            }
                            commands.Add(new SKParallelogramCommand()
                            {
                                ZIndex = 30 + d,
                                From = _from,
                                To = _to,
                                Fill = paint.DimensionLine(d, dl.AlphaType),
                                Width = theme.DimensionLine_Width
                            });
                        }
                    }
                }
            }


            foreach (var plk in cd.Pitchlines)
            {
                var pl = plk.Value;
                var plx = baseX;
                var plx2 = 0f;
                if (pl.offsetLength != null || pl.offsetLengthScale != null || pl.offsetXByScale != null || pl.offsetX != null)
                {
                    plx += (float)(pl.offsetXByScale.GetValueOrDefault(0) * theme.Pitchline_Width);
                    plx += (float)pl.offsetX.GetValueOrDefault(0);
                    plx2 = plx + (float)(pl.offsetLengthScale.GetValueOrDefault(1) * theme.Pitchline_Width);
                    plx2 += (float)pl.offsetLength.GetValueOrDefault(0);
                }
                else
                {
                    plx -= cd.HasDimensionlineAt(pl.Id, DimensionlinePosition.Left) ? 0 : theme.DimensionLine_Width;
                    plx2 = baseX + theme.Pitchline_Width;
                    plx2 += cd.HasDimensionlineAt(pl.Id, DimensionlinePosition.Right) ? 0 : theme.DimensionLine_Width;
                }
                var ply = (float)(-Math.Log2((cd.RootFormula.ResolveFrequency(v) * pl.Formula.RatioValue) / v.Initial) * v.Spacing_1D);

                commands.Add(new SKHorizontalLineCommand()
                {
                    ZIndex = 20,
                    LeftX = plx,
                    RightX = plx2,
                    Y = ply,
                    Stroke = paint.Pitchline(pl.Type, pl.AlphaType, pl.IsDotted),
                });
                if (pl.HasBaseTriangle != PitchlineSymbolPosition.None)
                {
                    commands.Add(new SKBaseTriangleCommand()
                    {
                        ZIndex = 25,
                        X = pl.HasBaseTriangle == PitchlineSymbolPosition.Left ? plx - 25 : plx2 + 25,
                        Y = ply,
                        Size = 10,
                        AngleDegrees = pl.HasBaseTriangle == PitchlineSymbolPosition.Left ? 0 : 180,
                        Stroke = paint.BaseTriangle(),
                        SourceId = cd.Id,
                        PitchlineId = pl.Id,
                    });
                }
            }
            return commands;
        }
    }
}
