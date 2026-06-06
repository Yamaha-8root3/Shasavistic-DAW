using Microtone.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score.Timelines.ScoreItems.PitchLine
{
    public class Pitchline
    {
        public Guid Id { get; internal set; }
        public OvertoneFormula Formula { get; internal set; } = new([0]);
        public bool IsIntermediate { get; set; } = false;
        /// <summary>
        /// 根音かどうか
        /// </summary>
        public bool IsBase { get; set; } = false;
        /// <summary>
        /// ChordDiaramのツリーの根本として扱うか
        /// </summary>
        public bool IsRoot { get; set; } = false;
        public bool IsDotted = false;
        public double? offsetX = null;
        public double? offsetLength = null;
        public double? offsetXByScale = null;
        public double? offsetLengthScale = null;
        public PitchlineType Type = PitchlineType.Normal;
        public AlphaType AlphaType = AlphaType.Normal;
        public PitchlineSymbolPosition HasNode = PitchlineSymbolPosition.None;
        public PitchlineSymbolPosition HasBaseTriangle = PitchlineSymbolPosition.None;
    }
}
