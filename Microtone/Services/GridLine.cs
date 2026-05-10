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

        public static GridLineColors BuildGridLineColors(int ppq, int division, ScoreRenderTheme theme)
        {
            var levels = new List<GridLineColor>();


            for (int i = 1; i <= division; i++)
            {
                int div = division / GCD(i, division); // i/divisionを約分して分母を求める
                //if (levels.Exists(l => l.StepDivisor == div)) continue; // すでに同じStepDivisorがある場合はスキップ
                if (IsPowerOfTwo(div) || div == 1) // 分母が2のべき乗か1
                {
                    int powerOfTwo = (int)(Math.Log(div, 2)); // 2の何乗か
                    if (powerOfTwo < theme.GridPow2Colors.Length)
                    {
                        levels.Add(new GridLineColor(i, theme.GridPow2Colors[powerOfTwo]));
                    }
                }
                else // それ以外は素数で判定
                {
                    long oddPrime = 1;
                    while (div % 2 == 0) div /= 2; // 2で割り切れる限り割る
                    for (int k = 0; k < theme.GridPrimeColors.Length; k++)
                    {
                        oddPrime = NextOddPrime(oddPrime);
                        if (div % oddPrime == 0)
                        {
                            levels.Add(new GridLineColor(i , theme.GridPrimeColors[k]));
                            break;
                        }
                    }
                }
            }


            //for (int i = 1; i <= division; i++)
            //{
            //    var powerof2 = 0;
            //    var i_ = i;
            //    while (i_ % 2 == 0)
            //    {
            //        powerof2++;
            //        i_ /= 2;
            //    }
            //    if ((powerof2 > 0 && i_ == 1) || i == 1) {
            //        //色指定がある場合のみ
            //        if (powerof2 < theme.GridPow2Colors.Length)
            //            levels.Add(new GridLineColor(division / i, theme.GridPow2Colors[powerof2]));
            //    }
            //    else
            //    {
            //        var oddprime = 1L;
            //        for (int k = 0; k < theme.GridPrimeColors.Length; k++)
            //        {
            //            oddprime = NextOddPrime(oddprime);
            //            if (i_ % oddprime == 0)
            //            {
            //                if (levels.Exists(l => l.StepDivisor == division / i)) break; // 2系でカバーされている位置は除外
            //                levels.Add(new GridLineColor(division / i, theme.GridPrimeColors[k]));
            //                break;
            //            }
            //        }
            //    }

            //}


            // StepDivisor 降順（粗い順）でソート → Render側で先頭から見て最初に割り切れた色を使う
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
