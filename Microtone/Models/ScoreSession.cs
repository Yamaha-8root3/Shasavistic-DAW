using Microtone.Interfaces.Editor;
using Microtone.Models.Rendering;
using Microtone.Models.Score;
using Microtone.Models.Score.Pitch;
using Microtone.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models
{
  internal class ScoreSession
  {
    public ScoreData Score { get; } = new(1);
    public ScoreRenderTheme Theme { get; set; } = new();
    public GridSettings Grid { get; set; } = new();
    public float PixelPerQuarter { get; set; } = 100f;
    public float Spacing_1D = 200f;

    public ScoreSession()
    {
      Score.TimeSignatureMap.Add(new(0, 120, 4, 4));
      Score.BenchMarkMap.Add(new(-200,
          new InitialCenteredPitch(new([0])),
          new InitialCenteredPitch(new([0]))));
      Score.ScorelineMap.Add(new(-200, [new(true, new([-1, 1]), false, true)]));
      Score.DefiningChordMap.Add(new(new([0, 1]), new([0, 0, 0, 1])));
    }

    public SKRenderData BuildRenderData(ISelectionState? selectionState, IReadOnlyDictionary<Guid, long>? tickOverrides = null)
        => DiagramTimelineRenderDataBuilder.Build(this, selectionState, tickOverrides);

    public SKRenderCommand BuildCursor(long tick)
        => DiagramTimelineRenderDataBuilder.BuildCursor(this, tick);
  }
}
