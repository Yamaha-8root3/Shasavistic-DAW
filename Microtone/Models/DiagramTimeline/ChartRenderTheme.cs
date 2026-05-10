using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.DiagramTimeline.Rendering
{
    public sealed class ChartRenderTheme
    {
        public event Action? Changed;
        public enum AlphaType
        {
            Normal = 0,
            Light = 1
        }
        public enum PitchLineType
        {
            None = -1,
            Normal = 0,
            Additional = 1,
        }
        //public enum DimensionLinePosition
        //{
        //    Left = 0,
        //    Right = 1,
        //    Center = 2,
        //}
        public SKColor Background { get; init; } = new SKColor(103, 102, 129);
        public float Spacing_1D { get; init; } = 200f;

        public Dimensions<SKColor> DimensionColor { get; init; } = new([
            new SKColor(254,254,254), // 0D tonic line
            new SKColor(254,254,254), // 1D
            new SKColor(242,121,146), // 2D
            new SKColor(108,217,133), // 3D
            new SKColor(181,152,238), // 4D
            new SKColor(255,194,71),  // 5D
            new SKColor(181,181,0),   // 6D
            new SKColor(237,152,119), // 7D
        ],new SKColor(255,255,255));
        public Dimensions<byte> ScoreLineAlpha { get; init; } = new([77, 38, 38, 38, 38, 38, 38],255);
        public Dimensions<int> ScorelineWidth{ get; init; } = new([5, 5, 5, 5, 5, 5, 5],5);

        public SKColor[] PitchLineColor { get; init; } = [
            new SKColor(255,255,255),
            new SKColor(124,216,226)];
        
        public byte[] PitchLineAlpha { get; init; } = [255,38];
        public float Pitchline_Width { get; init; } = 117f;
        public float Pitchline_Height { get; init; } = 5f;
        public byte[] DimensionLineAlpha { get; init; } = [255, 38];
        public float DimensionLine_Width { get; init; } = 10f;
        //public Dimensions<DimensionLinePosition> DimensionLine_Position { get; init; } = new(
        //    [DimensionLinePosition.Center,
        //     DimensionLinePosition.Center,
        //     DimensionLinePosition.Left,
        //     DimensionLinePosition.Right,
        //     DimensionLinePosition.Left,
        //     DimensionLinePosition.Right,
        //     DimensionLinePosition.Left,
        //     DimensionLinePosition.Right,
        //],DimensionLinePosition.Right);
        
        
        //Spacing * Ratio = Actual Spacing
        public Dimensions<float> DimensionRatio { get; init; } = new([
            0,
            MathF.Log2(2/1),     // 1D
            MathF.Log2(3f/2f),   // 2D
            MathF.Log2(5f/4f),   // 3D
            MathF.Log2(7f/4f),   // 4D
            MathF.Log2(11f/4f),  // 5D
            MathF.Log2(13f/8f),  // 6D
            MathF.Log2(17f/16f), // 7D
        ],0);

        public byte DiagramTimelineLayerCount { get; init; } = 5;
    }
}
