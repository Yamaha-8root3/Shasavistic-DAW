using Microtone.Interfaces.Score;
using Microtone.Interfaces.Score.Pitch;
using Microtone.Models.Enums;
using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using Microtone.Models.Score.Timelines.ScoreVariableItems;
using System;
using System.Collections.Generic;

namespace Microtone.Models.Score.Timelines.ScoreItems
{
    public enum NoteState
    {
        Default,
        Omit,
        Base,
        Add,
    }

    public enum TranstensionKind
    {
        None,
        Cra,    // v : 機能根・客音を保持して反転
        Vra,    // ^ : 司音・客音を保持して反転
        Na,     // x : 機能根・司音を保持して反転
        Crana,  // ; : 客音を保持して反転（詳細機能式でも使用可）
        Navra,  // : : 司音を保持して反転
    }

    public enum ComposeDirection
    {
        Above,  // + : 右側の底音を左側の和音の上に合成
        Below,  // \ : 左側の底音を右側の和音の下に合成
    }

    public enum ToneDimension
    {
        Root,   // 機能根
        Host,   // 司次元方向
        Guest,  // 客次元方向
    }

    /// <summary>
    /// ボイシングのキー。
    /// Step: 0=コア音, 1=+1個目, -1=-1個目（Guestのみ）。Root は常に Step=0。
    /// </summary>
    public record ToneKey(ToneDimension Dimension, int Step);

    /// <summary>
    /// 1つの構成音に対するボイシング展開。複数オクターブに展開可能。
    /// </summary>
    public class ToneVoicing
    {
        public List<int> OctaveOffsets { get; set; } = [0];
    }

    /// <summary>
    /// Move先・移動量の表現基底
    /// </summary>
    public abstract class MoveRef
    {
        public abstract IResolvablePitch Resolve(DefiningChord definingChord, BenchmarkKind benchmark);
    }

    /// <summary>整数による移動量。例）[0] [1] [-1]</summary>
    public class ModalMoveRef(int n) : MoveRef
    {
        public int Move { get; set; } = n;

        public override IResolvablePitch Resolve(DefiningChord definingChord, BenchmarkKind benchmark)
        {
            var res = new Harmonograph([]);
            //if ((definingChord.Host is null) || (definingChord.Guest is null)) TODO 何かしらエラー処理
            res[definingChord.Host.MaxDimension] = Move;
            switch (benchmark)
            {
                case BenchmarkKind.Modal:
                    return new ModalCenteredPitch(res);
                default:
                    return new ModalCenteredPitch(res);
            }
        }
    }

    public enum MoveBaseKind
    {
        Previous,   // 前の音
        ScoreRoot,  // スコアのroot
    }

    /// <summary>
    /// 機能式(Functograph)の基底クラス。
    /// </summary>
    public abstract class Functograph : ITimelineItem
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

        public BenchmarkKind BenchmarkKind { get; set; } = BenchmarkKind.Modal;

        public Dictionary<ToneKey, ToneVoicing> Voicing { get; set; } = [];

        //public abstract ChordDiagram ToChordDiagram();
    }

    /// <summary>
    /// 詳細機能式 (DetailedFunctograph)。
    /// 記述順：GuestDown | Move | Crana(;) | CoreNoteStates | HostUp / GuestUp
    /// 制約：Base は全配列通じて1つのみ。Add は CoreNoteStates に使用不可。
    /// </summary>
    public class DetailedFunctograph : Functograph
    {
        public MoveRef Move { get; set; } = new ModalMoveRef(0);
        public bool HasCrana { get; set; } = false;
        /// <summary>
        /// 機能根
        /// </summary>
        public NoteState Root { get; set; } = NoteState.Default;
        /// <summary>
        /// 司次元上昇方向 0番目は司音
        /// </summary>
        public NoteState[] HostUp { get; set; } = [NoteState.Default];
        /// <summary>
        /// 客次元上昇方向 0番目は客音
        /// </summary>
        public NoteState[] GuestUp { get; set; } = [NoteState.Default];
        /// <summary>
        /// 客次元下降方向 0番目は-1
        /// </summary>
        public NoteState[] GuestDown { get; set; } = [];

        public bool IsValid()
        {
            int baseCount = 0;
            if (Root == NoteState.Base) baseCount++;
            foreach (var s in HostUp) if (s == NoteState.Base) baseCount++;
            foreach (var s in GuestUp) if (s == NoteState.Base) baseCount++;
            foreach (var s in GuestDown) if (s == NoteState.Base) baseCount++;
            if (baseCount > 1) return false;

            if (Root == NoteState.Add) return false;
            if (HostUp[0] == NoteState.Add) return false;
            if (GuestUp[0] == NoteState.Add) return false;

            return true;
        }

        public Ratio GetBase(DefiningChord definingChord, Dimensions<OvertoneFormula> dimensionsDefinision)
        {
            return new();
        }

        public ChordDiagram ToChordDiagram(DefiningChord definingChord, Dimensions<int> dimensionsDefinision)
        {
            ChordDiagram diagram = new()
            {
                Id = this.Id,
                StartTick = this.StartTick,
                LengthTick = this.LengthTick,
                LengthKind = this.LengthKind,
                Isresolved = this.Isresolved,
            };
            if (!IsValid()) return diagram;
            if ((definingChord.Host is null) || (definingChord.Guest is null)) return diagram;

            diagram.RootFormula = Move.Resolve(definingChord, BenchmarkKind);


            var hostdim = definingChord.Host.MaxDimension;
            var guestdim = definingChord.Guest.MaxDimension;

            Harmonograph root = new([0]);
            Harmonograph host;
            Harmonograph guest;

            Guid rootpl;
            Guid? guestpl = null;

            Guid lastpl;

            Ratio bottom = new(int.MaxValue, 1);

            // Ah
            if (HasCrana)
            {
                root[hostdim]++;
                root[guestdim]--;
            }
            rootpl = diagram.AddPitchLine(root.ToOvertoneFormula(dimensionsDefinision), Root == NoteState.Omit)!.Id;
            //host up
            host = root.Clone();
            host += definingChord.Host;
            lastpl = rootpl;
            foreach (var item in HostUp)
            {
                var pl = diagram.AddPitchLine(host.ToOvertoneFormula(dimensionsDefinision), item == NoteState.Omit)!;
                if (item == NoteState.Add) pl.Type = PitchlineType.Additional;
                diagram.AddDimensionlineChain(lastpl, pl.Id, dimensionsDefinision);
                lastpl = pl.Id;
                host += definingChord.Host;
            }
            //guest up
            guest = root.Clone();
            guest += definingChord.Guest;
            lastpl = rootpl;
            foreach (var item in GuestUp)
            {
                var pl = diagram.AddPitchLine(guest.ToOvertoneFormula(dimensionsDefinision), item == NoteState.Omit)!;
                guestpl ??= pl.Id;
                if (item == NoteState.Add) pl.Type = PitchlineType.Additional;
                diagram.AddDimensionlineChain(lastpl, pl.Id, dimensionsDefinision);
                lastpl = pl.Id;
                guest += definingChord.Host;
            }
            //guest down
            guest = root.Clone();
            guest += definingChord.Guest;
            guest -= definingChord.Host;
            lastpl = (Guid)guestpl!;
            foreach (var item in GuestDown)
            {
                var pl = diagram.AddPitchLine(guest.ToOvertoneFormula(dimensionsDefinision), item == NoteState.Omit)!;
                if (item == NoteState.Add) pl.Type = PitchlineType.Additional;
                diagram.AddDimensionlineChain(lastpl, pl.Id, dimensionsDefinision);
                lastpl = pl.Id;
                guest -= definingChord.Host;
            }



            return diagram;
        }


    }

    /// <summary>
    /// 概略機能式 (OutlineFunctograph)。
    /// Move + TranstensionKind のみ。TranstensionKind と IsSandwiching は排他。
    /// </summary>
    public class OutlineFunctograph : Functograph
    {
        public MoveRef Move { get; set; } = new ModalMoveRef(0);
        public TranstensionKind Transtension { get; set; } = TranstensionKind.None;
        public bool IsSandwiching { get; set; } = false;

        public static bool IsValid() =>
            true;
    }

    /// <summary>
    /// スペース合成 (MergedFunctograph)。左が副機能、右が主機能。再帰可。最小2要素。
    /// </summary>
    public class MergedFunctograph : Functograph
    {
        public List<Functograph> Units { get; set; } = [];
        public bool IsValid() => Units.Count >= 2;
    }

    /// <summary>
    /// 上下合成 (ComposedFunctograph)。1回のみ。左が副機能、右が主機能。
    /// Above(+)：右側の底音を左側の和音の上に合成
    /// Below(\)：左側の底音を右側の和音の下に合成
    /// </summary>
    public class ComposedFunctograph : Functograph
    {
        public Functograph Base { get; set; } = null!;
        public MoveRef Added { get; set; } = new ModalMoveRef(0);
        public ComposeDirection Direction { get; set; }
        public ToneVoicing AddedVoicing { get; set; } = new();
    }

    /// <summary>
    /// タイムラインに乗る和音ノーツ。機能式が音高の権威。
    /// ChordDiagram は Functograph から生成されるキャッシュ。
    /// </summary>
    public class ChordNote : ITimelineItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public long StartTick { get; set; } = 0;
        public long LengthTick { get; set; } = 0;
        public LengthKind LengthKind { get; set; } = LengthKind.Fixed;
        public bool Isresolved { get; set; } = false;
        public double? TimeSeconds { get; set; } = null;
        public void UpdateTime(double timeSeconds) => TimeSeconds = timeSeconds;

        public Functograph Functograph { get; set; } = new DetailedFunctograph();

        /// <summary>見た目オーバーライド（PitchlineのIdをキー）</summary>
        public Dictionary<int, PitchlineOverride> Overrides { get; set; } = [];
    }

    /// <summary>Pitchline の見た目オーバーライド。音高には影響しない。</summary>
    public class PitchlineOverride
    {
        public double? OffsetX { get; set; } = null;
        public double? OffsetLength { get; set; } = null;
        public double? OffsetXByScale { get; set; } = null;
        public double? OffsetLengthScale { get; set; } = null;
        public PitchlineType? Type { get; set; } = null;
        public AlphaType? AlphaType { get; set; } = null;
        public PitchlineSymbolPosition? HasNode { get; set; } = null;
        public PitchlineSymbolPosition? HasBaseTriangle { get; set; } = null;
    }
}
