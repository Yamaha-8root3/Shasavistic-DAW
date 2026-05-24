using Microtone.Interfaces.Score;
using Microtone.Models.Score.Timelines.Caches;
using Microtone.Models.Score.Timelines.ScoreVariableItems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines
{
  public class TimeSignatureMap : ScoreTimeLine<TimeSignatureModifier>, IScoreTimeline
  {
    //private IReadOnlyList<TimeSignatureModifier> Ranges => Items;
    public int PPQ { get; } = 480;

    public double TickToSeconds(long tick)
    {
      var items = Items;
      double seconds = 0;
      long prevTick = 0;
      double prevBpm = 120;

      foreach (var item in items)
      {
        if (item.StartTick >= tick) break;
        seconds += (Math.Min(item.StartTick, tick) - prevTick) / (double)PPQ / prevBpm * 60.0;
        prevTick = item.StartTick;
        prevBpm = item.Bpm ?? prevBpm;
      }

      seconds += (tick - prevTick) / (double)PPQ / prevBpm * 60.0;
      return seconds;
    }

    public long SecondsToTick(double seconds)
    {
      var items = Items;
      double remaining = seconds;
      long tick = 0;
      double prevBpm = 120;

      for (int i = 0; i < items.Count; i++)
      {
        double bpm = items[i].Bpm ?? prevBpm;
        // このセクションの終端秒数
        double sectionEndSec = i + 1 < items.Count
            ? TickToSeconds(items[i + 1].StartTick) - TickToSeconds(items[i].StartTick)
            : double.MaxValue;

        if (remaining <= sectionEndSec)
        {
          tick = items[i].StartTick + (long)(remaining * PPQ * bpm / 60.0);
          return tick;
        }

        remaining -= sectionEndSec;
        prevBpm = bpm;
      }

      // セクションがない場合
      return (long)(seconds * PPQ * prevBpm / 60.0);
    }


    public (int bar, int beat, int subTick) TickToBarBeat(long tick)
    {
      var items = Items;
      TimeSignatureModifier? current = null;
      long sectionStart = 0;
      int barOffset = 1;

      for (int i = 0; i < items.Count; i++)
      {
        if (items[i].StartTick > Math.Abs(tick)) break;
        if (current != null)
        {
          long sectionTicks = items[i].StartTick - sectionStart;
          long ticksPerBar = PPQ * 4L * (current.BeatPerBar ?? 4) / (current.BeatType ?? 4);
          barOffset += (int)Math.Ceiling((double)sectionTicks / ticksPerBar);
        }
        current = items[i];
        sectionStart = items[i].StartTick;
      }

      if (current?.Bpm == null) return (1, 1, (int)Math.Abs(tick));

      int beatPerBar = current.BeatPerBar ?? 4;
      int beatType = current.BeatType ?? 4;
      long ticksPerBeat = PPQ * 4L / beatType;
      long ticksPerBar2 = ticksPerBeat * beatPerBar;

      int bar, beat, rem;

      if (tick < 0)
      {
        long absDelta = -tick - 1;
        bar = barOffset + (int)(absDelta / ticksPerBar2);
        long posInBar = absDelta % ticksPerBar2;
        long revPos = ticksPerBar2 - 1 - posInBar;
        beat = 1 + (int)(revPos / ticksPerBeat);
        rem = (int)(revPos % ticksPerBeat);
      }
      else
      {
        long delta = tick - sectionStart;
        bar = barOffset + (int)(delta / ticksPerBar2);
        beat = 1 + (int)(delta % ticksPerBar2 / ticksPerBeat);
        rem = (int)(delta % ticksPerBeat);
      }

      return (bar, beat, rem);
    }

    public long BarBeatToTick(int bar, int beat, int subTick = 0)
    {
      var items = Items;
      long sectionStart = 0;
      int barOffset = 1;
      TimeSignatureModifier? current = null;

      for (int i = 0; i < items.Count; i++)
      {
        if (current != null)
        {
          long ticksPerBar = PPQ * 4L * (current.BeatPerBar ?? 4) / (current.BeatType ?? 4);
          long nextBarOffset = barOffset + (long)Math.Ceiling((double)(items[i].StartTick - sectionStart) / ticksPerBar);
          if (nextBarOffset > Math.Abs(bar)) break;
          barOffset = (int)nextBarOffset;
        }
        current = items[i];
        sectionStart = items[i].StartTick;
      }

      if (current == null) return 0;
      int beatPerBar = current.BeatPerBar ?? 4;
      int beatType = current.BeatType ?? 4;
      long ticksPerBeat = PPQ * 4L / beatType;
      long ticksPerBar2 = ticksPerBeat * beatPerBar;

      if (bar < 0)
      {
        // TickToBarBeatの逆算
        // revPos = (beat-1)*ticksPerBeat + subTick
        // posInBar = ticksPerBar2 - 1 - revPos
        // absDelta = (bar-barOffset)*ticksPerBar2 + posInBar
        // tick = -(absDelta + 1)
        long revPos = (beat - 1) * ticksPerBeat + subTick;
        long posInBar = ticksPerBar2 - 1 - revPos;
        long absDelta = (long)(Math.Abs(bar) - barOffset) * ticksPerBar2 + posInBar;
        return -(absDelta + 1);
      }

      return sectionStart + (long)(bar - barOffset) * ticksPerBar2 + (beat - 1) * ticksPerBeat + subTick;
    }



  }
}
