using Microtone.Interfaces.Score.Pitch;
using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines.ScoreVariableItems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score
{
  public struct ScoreVariables
  {
    public int PPQ = 480;
    public float PixelPerQuarter = 200;
    public float Spacing_1D = 100;

    public long LastVariableChangedTick = long.MinValue;

    public double Initial = 261.63;
    public double BPM = 120;
    public int BeatPerBar = 4;
    public int BeatType = 4;
    public long StartTick = 0;
    public IResolvablePitch? Modal = null;
    public IResolvablePitch? Tonic = null;
    public IResolvablePitch? Resolutive = null;
    public List<ScorelineItem> ScoreLines = [];
    public Dimensions<int> Dimension1DOffset = new([]);
    public DefiningChord definingChord = new();
    public ScoreVariables()
    {
    }
  }
}
