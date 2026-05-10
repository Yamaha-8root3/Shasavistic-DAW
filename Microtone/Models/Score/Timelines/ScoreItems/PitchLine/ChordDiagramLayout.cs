using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreItems.PitchLine
{
    /// <summary>
    /// 描画位置のアンカー。
    /// 実座標 = BaseX + Offset + Scale * Pitchline_Width
    /// </summary>
    public class PitchlineRenderAnchor
    {
        public double Offset { get; set; } = 0.0;
        public double Scale { get; set; } = 0.0;
    }

    /// <summary>
    /// 始点・終点アンカーによる描画スパン。
    /// デフォルトは Scale=0～1（Pitchline_Width ぴったり）。
    /// </summary>
    public class PitchlineRenderSpan
    {
        public PitchlineRenderAnchor Start { get; set; } = new();
        public PitchlineRenderAnchor End { get; set; } = new() { Scale = 1.0 };
        public bool ShrinkLeft { get; set; } = false;
        public bool ShrinkRight { get; set; } = false;
    }



    /// <summary>
    /// 音高線1本の描画用成型済みデータ。
    /// - RenderSpan はデフォルトルール適用・PitchlineOverride 合成済み
    /// - IsDotted は Pitchline.IsIntermediate から導出
    /// - NodePosition / BaseTrianglePosition は HasDimensionlineAt から解決済み
    /// </summary>
    public class PitchlineLayout
    {
        public required Pitchline Source { get; init; }
        public PitchlineRenderSpan Span { get; init; } = new();

        /// <summary>中間音は点線。IsIntermediate から導出。</summary>
        public bool IsDotted { get; init; } = false;

        /// <summary>ノード記号の位置。有無は Source.HasNode、位置は ToLayout() で解決。</summary>
        public PitchlineSymbolPosition NodePosition { get; init; } = PitchlineSymbolPosition.None;

        /// <summary>底音三角の位置。有無は Source.HasBaseTriangle、位置は ToLayout() で解決。</summary>
        public PitchlineSymbolPosition BaseTrianglePosition { get; init; } = PitchlineSymbolPosition.None;
    }

    /// <summary>
    /// 次元線の接続点。音高線の長さを0.0～1.0の割合で指定。
    /// 0.0 = 音高線の左端、1.0 = 右端、0.5 = 中央
    /// さらに絶対オフセットを加算可能。
    /// </summary>
    public class DimensionlineRenderAnchor
    {
        public double T { get; set; } = 0.0;       // 音高線長さの割合
        public double Offset { get; set; } = 0.0;  // 絶対オフセット（ピクセル）
    }

    /// <summary>
    /// 次元線1本の描画用成型済みデータ。
    /// FromPosition / ToPosition は ToLayout() 時に Harmonograph から決定。
    /// </summary>
    public class DimensionlineLayout
    {
        public required Dimensionline Source { get; init; }
        public required PitchlineLayout Lower { get; init; }
        public required PitchlineLayout Higher { get; init; }
        /// <summary>Lower側の接続点</summary>
        public DimensionlineRenderAnchor LowerAnchor { get; init; } = new();
        /// <summary>Higher側の接続点</summary>
        public DimensionlineRenderAnchor HigherAnchor { get; init; } = new();
        /// <summary>何次元の次元線か（描画色・ZIndex に使用）</summary>
        public int Dimension { get; init; } = 1;
        public AlphaType AlphaType { get; init; }
    }

    /// <summary>
    /// ChordDiagram の描画用成型済みデータ。
    /// ChordDiagram.ToLayout() で生成。
    /// </summary>
    public class ChordDiagramLayout
    {
        public required ChordDiagram Source { get; init; }
        public IReadOnlyList<PitchlineLayout> Pitchlines { get; init; } = [];
        public IReadOnlyList<DimensionlineLayout> Dimensionlines { get; init; } = [];
    }
}
