using Microtone.Interfaces.Score;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using Microtone.Services;
using Microtone.Services.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microtone.Models
{
  internal class ScoreEditor(ScoreSession session)
  {
    private readonly ScoreSession _session = session;

    public long PixelToTick(double pixelX)
    {
      var v = _session.Score;
      return (long)(pixelX / _session.PixelPerQuarter * v.TimeSignatureMap.PPQ);
    }

    public long SnapTick(long tick)
        => _session.Grid.SnapEnabled
            ? GridSnapper.SnapTick(tick, _session.Score.TimeSignatureMap, _session.Grid.Division, 100)
            : tick;

    public long SnapDragOffset(long originalMinTick, long rawOffsetTick)
    {
      if (!_session.Grid.SnapEnabled) return rawOffsetTick;
      long representativeTick = originalMinTick + rawOffsetTick;
      long snapped = GridSnapper.SnapTick(representativeTick, _session.Score.TimeSignatureMap, _session.Grid.Division, 100);
      return snapped - originalMinTick;
    }

    // ChordDiagram配置（PlaceChordDiagramから）
    public ChordDiagram PlaceChordDiagram(long tick, List<Harmonograph> harmonographs)
    {
      var cd = new ChordDiagram { StartTick = tick, LengthTick = 240 };
      var baseFormula = harmonographs[0].ToOvertoneFormula(_session.Score.Dimension1DOffset);
      var basePl = cd.AddPitchLine(baseFormula)!;
      basePl.IsBase = true;

      foreach (var item in harmonographs)
      {
        var formula = item.ToOvertoneFormula(_session.Score.Dimension1DOffset);
        if (formula.Ratio.Value == baseFormula.Ratio.Value) continue;
        var plid = cd.AddPitchLine(formula)?.Id;
        if (plid is null) continue;
        cd.AddDimensionlineChain(basePl.Id, (int)plid, _session.Score.Dimension1DOffset);
      }

      _session.Score.ScoreTimeLines[0].Add(cd);
      return cd;
    }

    // ドラッグ確定（OnDragReleasedから）
    public void CommitDrag(DragSession dragSession)
    {
      long leftLimit = _session.Score.BenchMarkMap.Items[0].StartTick;
      foreach (var (id, originalTick) in dragSession.OriginalTicks)
      {
        var item = _session.Score.ScoreTimeLines
            .SelectMany(t => t.Items)
            .FirstOrDefault(i => i.Id == id);
        if (item != null)
          item.StartTick = Math.Max(leftLimit, originalTick + dragSession.SnappedOffsetTick);
      }
    }

    // アイテム逆引き
    public ITimelineItem? FindItem(Guid id)
        => _session.Score.ScoreTimeLines
            .SelectMany(t => t.Items)
            .FirstOrDefault(i => i.Id == id);
  }
}
