using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Microsoft.VisualBasic;
using Microtone.Interfaces.Editor;
using Microtone.Interfaces.Score;
using Microtone.Interfaces.Score.Pitch;
using Microtone.Models;
using Microtone.Models.Enums;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.SKRenderCommands;
using Microtone.Models.Score;
using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines.ScoreItems;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using Microtone.Models.Score.Timelines.ScoreVariableItems;
using Microtone.Services.Editor.Selection;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using static Microtone.Services.GridLine;

namespace Microtone.Services
{
  internal class DiagramTimelineRenderDataBuilder
  {
    public static SKRenderData Build(ScoreSession session, ISelectionState? selectionState = null, IReadOnlyDictionary<Guid, long>? displayTickOverrides = null)
    {
      var score = session.Score;
      var theme = session.Theme;
      var grid = session.Grid;

      var paint = new PaintFactory(theme);
      var data = new SKRenderData()
      {
        Background = paint.Background(),
      };

      data.Commands.Add(new SKHorizontalLineCommand()
      {
        Stroke = paint.BenchMarkLine(BenchmarkKind.Initial),
        ZIndex = 1,
        SourceItem = null,
        Y = 0,
      });

      if (score.TimeSignatureMap.Items.Count == 0)
      {
        return data;
      }
      else if (score.TimeSignatureMap.Items[0].Bpm == null ||
                score.TimeSignatureMap.Items[0].BeatPerBar == null ||
                score.TimeSignatureMap.Items[0].BeatType == null)
      {
        return data;
      }
      foreach (var item in score.ScoreTimeLines)
      {
        item.ResolveEnd();
      }

      var timeline = ScoreTimelineMerger.Merge(score);
      var v = new ScoreVariables()
      {
        PPQ = score.TimeSignatureMap.PPQ,
        Spacing_1D = session.Spacing_1D,
        PixelPerQuarter = session.PixelPerQuarter,
        Initial = score.BenchMarkMap.Initial,
        Dimension1DOffset = score.Dimension1DOffset
      };

      // signature情報は負の値の処理のために最初の定義を先に使う
      var initialsignature = score.TimeSignatureMap.Items[0];
      v.BPM = initialsignature.Bpm ?? v.BPM;
      v.BeatPerBar = initialsignature.BeatPerBar ?? v.BeatPerBar;
      v.BeatType = initialsignature.BeatType ?? v.BeatType;

      long gridSectionStartTick = score.TimeSignatureMap.Items[0].StartTick;
      int totalSteps0 = v.BeatPerBar * grid.Division / v.BeatType;
      GridLineColors? gridColors = (grid?.ShowGrid == true)
          ? BuildGridLineColors(totalSteps0, theme)
          : null;

      foreach (var item in timeline)
      {
        switch (item)
        {
          case TimeSignatureModifier ts:
            if (gridColors != null) // ここまでのGridを描画
              data.Commands.Add(BuildGridCommand(gridSectionStartTick, ts.StartTick, v, gridColors, grid!.Division, theme));
            data.Commands.AddRange(RenderScoreLinesBefore(ts.StartTick, v.ScoreLines, v, paint, theme));
            v.BPM = ts.Bpm ?? v.BPM;
            v.BeatPerBar = ts.BeatPerBar ?? v.BeatPerBar;
            v.BeatType = ts.BeatType ?? v.BeatType;
            v.StartTick = ts.StartTick;
            v.LastVariableChangedTick = ts.StartTick;
            gridSectionStartTick = ts.StartTick;

            if (gridColors != null)
            { // 次のGridデータの生成
              int totalSteps1 = v.BeatPerBar * grid!.Division / v.BeatType;
              gridColors = BuildGridLineColors(totalSteps1, theme);
            }
            break;

          case BenchMarkModifier bm:
            data.Commands.AddRange(RenderScoreLinesBefore(bm.StartTick, v.ScoreLines, v, paint, theme));
            if (bm.Resolutive != null)
            {
              v.Resolutive = PitchCalculator.CalcNextPitch(v, bm.Resolutive);
            }
            if (bm.Modal != null)
            {
              v.Modal = PitchCalculator.CalcNextPitch(v, bm.Modal);
            }
            data.Commands.Add(new SKVerticalLineCommand
            {
              ZIndex = 5,
              SourceItem = bm,
              X = bm.StartTick / v.PPQ * v.PixelPerQuarter,
              Stroke = paint.Splitter()
            });
            v.LastVariableChangedTick = bm.StartTick;
            break;

          case ScorelineModifier sm:
            data.Commands.AddRange(RenderScoreLinesBefore(sm.StartTick, v.ScoreLines, v, paint, theme));

            if (sm.ModifyType == ModifyType.Add)
            {
              v.ScoreLines.AddRange(sm.Lines);
            }
            else
            {
              v.ScoreLines = sm.Lines;
            }
            v.LastVariableChangedTick = sm.StartTick;
            break;

          case DefiningChord dfc:
            v.definingChord = dfc;
            break;

          case ChordDiagram cd:
            var cdselected = (selectionState as ChordDiagramSelection)?.Ids.Contains(cd.Id) == true ||
                             (selectionState as PitchlineSelection)?.ParentChordDiagramId == cd.Id;
            long? cd_overrideTick = null;
            if (displayTickOverrides?.TryGetValue(cd.Id, out var cd_ot) == true)
              cd_overrideTick = cd_ot;
            data.Commands.AddRange(ChordDiagramFactory.BuildChordDiagramWithLayout(cd, v, theme, paint,
              cdselected ? selectionState : null, cd_overrideTick));

            break;

          case DetailedFunctograph f:
            var fselected = (selectionState as ChordDiagramSelection)?.Ids.Contains(f.Id) == true ||
                            (selectionState as PitchlineSelection)?.ParentChordDiagramId == f.Id;
            long? f_overrideTick = null;
            if (displayTickOverrides?.TryGetValue(f.Id, out var f_ot) == true)
              f_overrideTick = f_ot;
            data.Commands.AddRange(ChordDiagramFactory.BuildChordDiagramWithLayout(f.ToChordDiagram(v.definingChord, v.Dimension1DOffset),
                                                                        v, theme, paint, fselected ? selectionState : null, f_overrideTick));
            break;


        }
      }

      long endTick = timeline.Last().StartTick + 4 * v.PPQ;
      if (gridColors != null)
        data.Commands.Add(BuildGridCommand(gridSectionStartTick, endTick, v, gridColors, grid!.Division, theme));
      data.Commands.AddRange(RenderScoreLinesBefore(endTick, v.ScoreLines, v, paint, theme));

      data.SortCommandsByZIndex();
      return data;
    }
    private static long GetEndTick(ITimelinePoint item)
    {
      if (item is ITimelineItem timelineItem)
      {
        return timelineItem.LengthKind switch
        {
          LengthKind.Fixed or LengthKind.UntilNext => timelineItem.StartTick + timelineItem.LengthTick,
          LengthKind.None => long.MaxValue,
          _ => timelineItem.StartTick,
        };
      }
      else
      {
        return long.MaxValue;
      }
    }

    private static List<SKRenderCommand> RenderScoreLinesBefore(long renderTickTo, List<ScorelineItem> items, ScoreVariables v, PaintFactory paint, ScoreRenderTheme theme)
    {
      if (renderTickTo == v.LastVariableChangedTick) return [];
      var commands = new List<SKRenderCommand>();
      foreach (ScorelineItem item in items)
      {
        if (v.Resolutive is null) continue;
        commands.Add(new SKHorizontalLineCommand
        {
          Stroke = paint.Scoreline(Math.Max(Math.Max(item.Offset.MaxDimension, item.Interval.MaxDimension), 1)),
          ZIndex = 2,
          SourceItem = item,
          Y = -(float)Math.Log2((v.Resolutive.ResolveFrequency(v) * item.Offset.RatioValue / v.Initial)) * v.Spacing_1D,
          Repeat = item.Infinity,
          RepeatInterval = item.Infinity == true ? -(float)Math.Log2(item.Interval.RatioValue) * v.Spacing_1D : 1,
          LeftX = (float)v.LastVariableChangedTick / v.PPQ * v.PixelPerQuarter,
          RightX = MathF.Min(renderTickTo, GetEndTick(item)) / v.PPQ * v.PixelPerQuarter,
          IsOriented = item.IsOriented,
          IgnoreOrigin = item.IgnoreOrigin,
        });
      }
      if (v.Resolutive != null)
      {
        commands.Add(new SKHorizontalLineCommand
        {
          Stroke = paint.BenchMarkLine(BenchmarkKind.Resolutive),
          ZIndex = 5,
          SourceItem = null,
          Y = -(float)Math.Log2((v.Resolutive.ResolveFrequency(v) / v.Initial)) * v.Spacing_1D,
          LeftX = (float)v.LastVariableChangedTick / v.PPQ * v.PixelPerQuarter,
          RightX = (float)renderTickTo / v.PPQ * v.PixelPerQuarter
        });
      }
      return commands;
    }

    public static SKRenderCommand BuildCursor(ScoreSession session, long cursorTick)
    {
      var score = session.Score;
      var theme = session.Theme;
      float cursorX = (float)cursorTick / score.TimeSignatureMap.PPQ * session.PixelPerQuarter;
      return new SKVerticalLineCommand
      {
        Stroke = new SKPaint
        {
          Color = theme.SelectionBorderColor,
          StrokeWidth = 2,
          IsAntialias = true,
          Style = SKPaintStyle.Stroke,
        },
        ZIndex = 1000,
        SourceItem = null,
        X = cursorX
       };
    }
  }
}
