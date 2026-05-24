using Microtone.Interfaces.Score;
using Microtone.Interfaces.Score.Pitch;
using Microtone.Models.Enums;
using Microtone.Models.Score.Pitch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microtone.Models.Score.Timelines.ScoreItems.PitchLine
{
  public class ChordDiagram : ITimelineItem
  {
    public Guid Id { get; set; } = Guid.NewGuid();
    public long StartTick { get; set; } = 0;
    public long LengthTick { get; set; } = 0;
    public LengthKind LengthKind { get; set; } = LengthKind.Fixed;
    public bool Isresolved { get; set; } = false;
    public double? TimeSeconds { get; set; } = null;
    public void UpdateTime(double timeSeconds)
    {
      TimeSeconds = timeSeconds;
    }

    private static (int, int) NormalizeKey(int a, int b) => a < b ? (a, b) : (b, a);

    //root
    public IResolvablePitch RootFormula { get; set; } = new ModalCenteredPitch(new([0]));
    //ID振るためのもの
    private int _nextPitchlineId = 0;
    private int _nextDimensionlineId = 0;

    private readonly Dictionary<int, Pitchline> _pitchlines = [];
    private readonly Dictionary<Ratio, int> _pitchlinesByRatio = [];
    private readonly Dictionary<int, Dimensionline> _dimensionlines = [];
    private readonly Dictionary<(int, int), int> _dimensionlinesByPitchIds = [];

    public IReadOnlyDictionary<int, Pitchline> Pitchlines => _pitchlines;
    public IReadOnlyDictionary<int, Dimensionline> Dimensionlines => _dimensionlines;

    public Dictionary<int, PitchlineOverride> PitchlineOverrides { get; } = [];
    public Dictionary<int, DimensionlineOverride> DimensionlineOverrides { get; } = [];
    public Pitchline? AddPitchLine(OvertoneFormula formula, bool isIntermediate = false)
    {
      var r = formula.Ratio.ToRatio;
      if (_pitchlinesByRatio.TryGetValue(r, out var existingId))
        if (isIntermediate) return null;
        else if (!_pitchlines[existingId].IsIntermediate)
        {
          _pitchlines[existingId].IsIntermediate = true;
          return _pitchlines[existingId];
        }
        else return null;

      int id = _nextPitchlineId++;
      var pitch = new Pitchline
      {
        Id = id,
        Formula = formula.Clone(),
        IsIntermediate = isIntermediate,
        IsDotted = isIntermediate
      };

      _pitchlines[id] = pitch;
      _pitchlinesByRatio[r] = (id);

      return _pitchlines[id];
    }
    public void RemovePitchLine(int id)
    {
      if (!_pitchlines.TryGetValue(id, out var pitchLine))
        return;

      // 関連する接続線も削除
      var relatedConnections = _dimensionlines.Values
          .Where(c => c.PitchIds.Item1 == id || c.PitchIds.Item2 == id)
          .Select(c => c.Id)
          .ToList();

      foreach (var connId in relatedConnections)
        RemoveDimensionline(connId);

      _pitchlinesByRatio.Remove(pitchLine.Formula.Ratio.ToRatio);
      _pitchlines.Remove(id);
      PitchlineOverrides.Remove(id);
    }
    public void UpdatePitch(int id, OvertoneFormula newformula)
    {
      var newratio = newformula.Ratio.ToRatio;
      if (_pitchlinesByRatio.ContainsKey(newratio))
        throw new InvalidOperationException("この音高は既に存在します");

      var pitchLine = _pitchlines[id];
      var oldPitch = pitchLine.Formula;
      var oldratio = oldPitch.Ratio.ToRatio;

      _pitchlinesByRatio.Remove(oldratio);
      _pitchlinesByRatio[newratio] = id;

      pitchLine.Formula = newformula;
    }
    public int? FindPitchLineByPitch(Ratio pitch)
    {
      return _pitchlinesByRatio.TryGetValue(pitch, out var id) ? id : null;
    }
    public bool HasDimensionlineAt(int id, DimensionlinePosition position)
    {
      return _dimensionlines.Values.Any(dl =>
          (dl.PitchIds.Item1 == id && dl._FromPosition == position) ||
          (dl.PitchIds.Item2 == id && dl._ToPosition == position)
      );
    }
    public List<Dimensionline> AddDimensionlineChain(int a, int b, Dimensions<int> dimension1DOffset)
    {
      var key = NormalizeKey(a, b);
      if (_dimensionlinesByPitchIds.TryGetValue(key, out int value))
        return [];

      if (_pitchlines[a].Formula.RatioValue > _pitchlines[b].Formula.RatioValue)
        (a, b) = (b, a);
      var higher = _pitchlines[b];
      var lower = _pitchlines[a];

      var interval = new Harmonograph(higher.Formula - lower.Formula, dimension1DOffset);
      if (interval.IsSingleStep)
      {
        int id = _nextDimensionlineId++;
        var dl = new Dimensionline { Id = id, PitchIds = key };
        _dimensionlines[id] = dl;
        _dimensionlinesByPitchIds[key] = id;
        return [_dimensionlines[id]];
      }
      else
      {
        var currentpitch = lower.Formula.Clone();
        var currentpitchlineid = lower.Id;
        List<Dimensionline> response = [];

        // 2次元から順に処理
        for (int i = 2; i <= interval.MaxDimension; i++)
        {
          if (currentpitch.MaxDimension < i) currentpitch[i] = 0;
          var k = interval[i];
          if (k == 0) continue;
          int step = Math.Sign(k);
          for (int j = 0; j < Math.Abs(k); j++)
          {
            currentpitch[i] += step;
            currentpitch[1] += dimension1DOffset[i]! * step;

            if (_pitchlinesByRatio.TryGetValue(currentpitch.Ratio.ToRatio, out int pl))
              if (_dimensionlinesByPitchIds.ContainsKey((currentpitchlineid, pl)))
              { currentpitchlineid = pl; continue; }
            var newpitchline = AddPitchLine(currentpitch, isIntermediate: true);
            if (newpitchline is null) continue;
            newpitchline.IsDotted = true;
            int id = _nextDimensionlineId++;
            var dl = new Dimensionline
            {
              Id = id,
              PitchIds = (currentpitchlineid, newpitchline.Id),
            };
            _dimensionlines[id] = dl;
            _dimensionlinesByPitchIds[(currentpitchlineid, newpitchline.Id)] = id;
            currentpitchlineid = newpitchline.Id;
            response.Add(dl);
          }
        }

        // 1次元の処理（最後、終点除く）
        {
          var k = interval[1];
          if (k != 0)
          {
            int step = Math.Sign(k);
            for (int j = 0; j < Math.Abs(k) - 1; j++)
            {
              currentpitch[1] += step;

              if (_pitchlinesByRatio.TryGetValue(currentpitch.Ratio.ToRatio, out int pl))
                if (_dimensionlinesByPitchIds.ContainsKey((currentpitchlineid, pl)))
                { currentpitchlineid = pl; continue; }
              var newpitchline = AddPitchLine(currentpitch, isIntermediate: true)!;
              if (newpitchline is null) continue;
              newpitchline.IsDotted = true;
              int id = _nextDimensionlineId++;
              var dl = new Dimensionline
              {
                Id = id,
                PitchIds = (currentpitchlineid, newpitchline.Id),
              };
              _dimensionlines[id] = dl;
              _dimensionlinesByPitchIds[(currentpitchlineid, newpitchline.Id)] = id;
              currentpitchlineid = newpitchline.Id;
              response.Add(dl);
            }
          }
        }

        // 終点と結合
        if (_dimensionlinesByPitchIds.ContainsKey((currentpitchlineid, higher.Id))) return response;
        int id_ = _nextDimensionlineId++;
        var dl_ = new Dimensionline
        {
          Id = id_,
          PitchIds = (currentpitchlineid, higher.Id)
        };
        _dimensionlines[id_] = dl_;
        _dimensionlinesByPitchIds[key] = id_;
        response.Add(dl_);
        return response;
      }
    }


    public void RemoveDimensionline(int id)
    {
      if (!_dimensionlines.TryGetValue(id, out var dl))
        return;

      _dimensionlinesByPitchIds.Remove(dl.PitchIds);
      _dimensionlines.Remove(id);
      DimensionlineOverrides.Remove(id);
    }
    public int? FindDimensionline(int a, int b) =>
        _dimensionlinesByPitchIds.TryGetValue(NormalizeKey(a, b), out var id) ? id : null;

    public class DimensionlinePath
    {
      public required Pitchline Higher { get; set; }
      public required Pitchline Lower { get; set; }
    }
    public DimensionlinePath GetDimensionlinePath(int dimensionlineId)
    {
      if (!_dimensionlines.TryGetValue(dimensionlineId, out var dl))
        throw new InvalidOperationException("次元線が見つかりません");
      Pitchline a = _pitchlines[dl.PitchIds.Item1];
      Pitchline b = _pitchlines[dl.PitchIds.Item2];
      if (a.Formula.RatioValue > b.Formula.RatioValue)
        return new DimensionlinePath { Higher = a, Lower = b };
      else
        return new DimensionlinePath { Higher = b, Lower = a };
    }

    /// <summary>
    /// 指定した Pitchline が Lower 側として接続している次元番号セット
    /// </summary>
    public IEnumerable<int> GetDimensionsAsLower(int pitchlineId, Dimensions<int> offset1d)
    {
      foreach (var dl in _dimensionlines.Values)
      {
        var path = GetDimensionlinePath(dl.Id);
        if (path.Lower.Id != pitchlineId) continue;
        var harmonograph = new Harmonograph(path.Higher.Formula - path.Lower.Formula, offset1d);
        for (int i = 1; i <= harmonograph.MaxDimension; i++)
          if (harmonograph[i] != 0) { yield return i; break; }
      }
    }

    /// <summary>
    /// 指定した Pitchline が Higher 側として接続している次元番号セット
    /// </summary>
    public IEnumerable<int> GetDimensionsAsHigher(int pitchlineId, Dimensions<int> offset1d)
    {
      foreach (var dl in _dimensionlines.Values)
      {
        var path = GetDimensionlinePath(dl.Id);
        if (path.Higher.Id != pitchlineId) continue;
        var harmonograph = new Harmonograph(path.Higher.Formula - path.Lower.Formula, offset1d);
        for (int i = 1; i <= harmonograph.MaxDimension; i++)
          if (harmonograph[i] != 0) { yield return i; break; }
      }
    }

    public ChordDiagramLayout ToLayout(
            ScoreVariables v,
            ScoreRenderTheme theme)
    {
      var pitchlineLayouts = new Dictionary<int, PitchlineLayout>();

      // 1. 各 Pitchline の PitchlineLayout を生成
      foreach (var (id, pl) in Pitchlines)
      {
        var lowerDims = GetDimensionsAsLower(pl.Id, v.Dimension1DOffset).ToList();
        var higherDims = GetDimensionsAsHigher(pl.Id, v.Dimension1DOffset).ToList();

        // Lower側 = fromT、Higher側 = toT で判定
        bool hasLeft = lowerDims.Any(d => theme.DimensionLineFromT[d] == 0.0)
                     || higherDims.Any(d => theme.DimensionLineToT[d] == 0.0);
        bool hasRight = lowerDims.Any(d => theme.DimensionLineFromT[d] == 1.0)
                     || higherDims.Any(d => theme.DimensionLineToT[d] == 1.0);

        var spanStart = new PitchlineRenderAnchor
        {
          Scale = 0.0,
        };
        var spanEnd = new PitchlineRenderAnchor
        {
          Scale = 1.0,
        };


        //TODO 右にBaseTriangleを自動設置する判定
        var baseTrianglePos = pl.IsBase
            ? (PitchlineSymbolPosition.Left)
            : PitchlineSymbolPosition.None;

        //TODO HasNodeの判定
        var nodePos = PitchlineSymbolPosition.None;
        //    ? (hasRight ? PitchlineSymbolPosition.Left : PitchlineSymbolPosition.Right)
        //    : PitchlineSymbolPosition.None;

        var ov = PitchlineOverrides.GetValueOrDefault(pl.Id);
        pitchlineLayouts[pl.Id] = new PitchlineLayout
        {
          Source = pl,
          IsDotted = pl.IsIntermediate,
          Span = ov?.Span ?? new PitchlineRenderSpan { Start = spanStart, End = spanEnd, ShrinkLeft = hasLeft, ShrinkRight = hasRight },
          BaseTrianglePosition = ov?.BaseTrianglePosition ?? baseTrianglePos,
          NodePosition = ov?.NodePosition ?? nodePos,
        };
      }

      // 2. 各 Dimensionline の DimensionlineLayout を生成
      var dimensionlineLayouts = new List<DimensionlineLayout>();
      foreach (var (id, dl) in Dimensionlines)
      {
        var path = GetDimensionlinePath(dl.Id);
        var lower = pitchlineLayouts[path.Lower.Id];
        var higher = pitchlineLayouts[path.Higher.Id];

        var interval = path.Higher.Formula - path.Lower.Formula;
        var harmonograph = new Harmonograph(interval, v.Dimension1DOffset);

        // 1次元1ステップ前提：非ゼロの次元を1つ取得
        int dim = 1;
        for (int i = 1; i <= harmonograph.MaxDimension; i++)
          if (harmonograph[i] != 0) { dim = i; break; }

        // T値をテーマから取得
        double fromT = theme.DimensionLineFromT[dim];
        double toT = theme.DimensionLineToT[dim];

        DimensionlineRenderAnchor loweranchor = new()
        {
          T = fromT,
        };
        DimensionlineRenderAnchor higheranchor = new()
        {
          T = toT,
        };

        var ov = DimensionlineOverrides.GetValueOrDefault(dl.Id);
        dimensionlineLayouts.Add(new DimensionlineLayout
        {
          Source = dl,
          Lower = lower,
          Higher = higher,
          LowerAnchor = ov?.LowerAnchor ?? loweranchor,
          HigherAnchor = ov?.HigherAnchor ?? higheranchor,
          Dimension = dim,
        });
      }

      return new ChordDiagramLayout
      {
        Source = this,
        Pitchlines = [.. pitchlineLayouts.Values],
        Dimensionlines = dimensionlineLayouts,
      };
    }

  }
}
