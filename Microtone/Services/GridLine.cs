using HarfBuzzSharp;
using Microtone.Models.Rendering.SKRenderCommands;
using Microtone.Models.Score;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services
{
    public class GridLine
    {
        public record GridLineColor(int StepDivisor, SKColor Color);
        // StepDivisor: fineStep が何個ごとにこの色か
        public record GridLineColors(GridLineColor[] Levels, SKColor Fallback);

    /// <param name="totalSteps">1小節あたりの最小グリッド数 = BeatPerBar * division / BeatType</param>
    public static GridLineColors BuildGridLineColors(int totalSteps, ScoreRenderTheme theme)
    {
      var levels = new List<GridLineColor>();

      for (int div = totalSteps; div >= 1; div--)
      {
        if (totalSteps % div != 0) continue;

        int noteUnit = totalSteps / div;

        if (IsPowerOfTwo(noteUnit))
        {
          int pow = (int)Math.Log2(noteUnit);
          if (pow < theme.GridPow2Colors.Length)
            levels.Add(new GridLineColor(div, theme.GridPow2Colors[pow]));
        }
        else
        {
          int reduced = noteUnit;
          while (reduced % 2 == 0) reduced /= 2;
          long oddPrime = 1;
          for (int k = 0; k < theme.GridPrimeColors.Length; k++)
          {
            oddPrime = NextOddPrime(oddPrime);
            if (reduced % oddPrime == 0)
            {
              levels.Add(new GridLineColor(div, theme.GridPrimeColors[k]));
              break;
            }
          }
        }
      }

      levels.Sort((a, b) => b.StepDivisor.CompareTo(a.StepDivisor));
      return new GridLineColors([.. levels], theme.GridFallbackColor);
    }
    public static GridLineColors BuildGridLineColors(int ppq, int division, ScoreRenderTheme theme)
    {
      var levels = new List<GridLineColor>();

      for (int div = division; div >= 1; div--)
      {
        if (division % div != 0) continue;

        // division/div = 何分音符か（1=全音符, 2=2分, 4=4分...）
        int noteUnit = division / div;

        if (IsPowerOfTwo(noteUnit))
        {
          int pow = (int)Math.Log2(noteUnit);
          if (pow < theme.GridPow2Colors.Length)
            levels.Add(new GridLineColor(div, theme.GridPow2Colors[pow]));
        }
        else
        {
          int reduced = noteUnit;
          while (reduced % 2 == 0) reduced /= 2;
          long oddPrime = 1;
          for (int k = 0; k < theme.GridPrimeColors.Length; k++)
          {
            oddPrime = NextOddPrime(oddPrime);
            if (reduced % oddPrime == 0)
            {
              levels.Add(new GridLineColor(div, theme.GridPrimeColors[k]));
              break;
            }
          }
        }
      }

      levels.Sort((a, b) => b.StepDivisor.CompareTo(a.StepDivisor));
      return new GridLineColors([.. levels], theme.GridFallbackColor);
    }

    public static SKGridCommand BuildGridCommand(
            long sectionStartTick, long sectionEndTick,
            ScoreVariables v, GridLineColors colors, int division, ScoreRenderTheme theme)
        {
            float pixelPerTick = v.PixelPerQuarter / v.PPQ;
            return new SKGridCommand
            {
                ZIndex = 1,
                SourceItem = null,
                StartX = sectionStartTick * pixelPerTick,
                EndX = sectionEndTick * pixelPerTick,
                SectionStartX = sectionStartTick * pixelPerTick,
                FinePixelInterval = v.PixelPerQuarter * 4f / division, // ← division 必要
                Colors = colors,
                StrokeWidth = theme.GridStrokeWidth,
            };
        }

        // ユーティリティ
        private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
        private static bool IsPrime(long n)
        {
            if (n < 2) return false;
            for (long i = 2; i * i <= n; i++) if (n % i == 0) return false;
            return true;
        }
        private static long NextOddPrime(long n)
        {
            for (long c = n + 2; ; c += 2) if (IsPrime(c)) return c;
        }
        // 3→0, 5→1, 7→2, 11→3, ...
        
        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
    }
}
