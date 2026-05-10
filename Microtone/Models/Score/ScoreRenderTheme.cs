using Microtone.Models.Enums;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score
{
    public struct ScoreRenderTheme
    {
        public ScoreRenderTheme() { }
        public event Action? Changed;
       
        //public enum DimensionLinePosition
        //{
        //    Left = 0,
        //    Right = 1,
        //    Center = 2,
        //}
        public SKColor Background = new(103, 102, 129);
        public float Spacing_1D = 200f;
        public float PixelPerQuarter = 100f;

        public Dimensions<SKColor> DimensionColor = new([
            new SKColor(254,254,254), // 1D
            new SKColor(242,121,146), // 2D
            new SKColor(108,217,133), // 3D
            new SKColor(181,152,238), // 4D
            new SKColor(255,194,71),  // 5D
            new SKColor(181,181,0),   // 6D
            new SKColor(237,152,119), // 7D
        ], new SKColor(255, 255, 255));

        public SKColor[] BenchMarkLineColor = [
            new(254, 254, 254, 19),
            new(254, 254, 254, 77),
            new(254, 254, 254, 77),
        ];
        public float[] BenchMarkLineWidth = [50, 5, 5];

        public SKColor SplitterColor = new(255, 255, 255, 38);
        public float SplitterWidth = 50f;

        public Dimensions<byte> ScoreLineAlpha = new([38, 38, 38, 38, 38, 38, 38],255);
        public Dimensions<int> ScorelineWidth = new([5, 5, 5, 5, 5, 5, 5],5);

        public SKColor[] PitchLineColor = [
            new SKColor(255,255,255),
            new SKColor(124,216,226)];
        public byte[] PitchLineAlpha = [255,38];
        public float Pitchline_Width = 100f;
        public float Pitchline_Height = 5f;
        public byte[] DimensionLineAlpha = [255, 38];
        public float DimensionLine_Width = 10f;

        public SKColor BaseTriangleColor = new(254, 254, 254);
        public float BaseTriangleStrokeWidth = 3;

        public SKColor SelectionColor = new(58, 57, 67, 80);
        public SKColor SelectionBorderColor = new(254, 254, 254, 80);


        public Dimensions<DimensionlinePosition> DimensionLineFromPosition = new(
            [
             DimensionlinePosition.Center,
             DimensionlinePosition.Left,
             DimensionlinePosition.Right,
             DimensionlinePosition.Left,
             DimensionlinePosition.Right,
             DimensionlinePosition.Left,
             DimensionlinePosition.Right,
        ], DimensionlinePosition.Right);
        public Dimensions<DimensionlinePosition> DimensionLineToPosition = new(
            [
             DimensionlinePosition.Center,
             DimensionlinePosition.Left,
             DimensionlinePosition.Right,
             DimensionlinePosition.Right,
             DimensionlinePosition.Left,
             DimensionlinePosition.Left,
             DimensionlinePosition.Right,
        ], DimensionlinePosition.Right);
        //T値は音高線の長さに対する割合。0.0 = 左端、1.0 = 右端、0.5 = 中央
        public Dimensions<double> DimensionLineFromT = new([0.5, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0], 1.0);
        public Dimensions<double> DimensionLineToT = new([0.5, 0.0, 1.0, 1.0, 0.0, 0.0, 1.0], 1.0);

        // 2^n分のグリッド
        public SKColor[] GridPow2Colors = [
            new(255, 255, 255, 90),  // 全音符(2^0)
            new(255, 255, 255, 50),  // 2分(2^1)
            new(255, 255, 255, 10),  // 4分(2^2)
            new(255, 255, 255, 5),  // 8分(2^3)
        ];
        //素数分のグリッド
        public SKColor[] GridPrimeColors = [
            new(242,121,146, 50),  // 3
            new(108,217,133, 40),  // 5
            new(181,152,238, 30),  // 7
        ];

        public SKColor GridFallbackColor = new(255, 255, 255, 15);
        public float GridStrokeWidth = 3f;
    }
}
