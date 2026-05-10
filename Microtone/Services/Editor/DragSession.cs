using Microtone.Interfaces.Score;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microtone.Services.Editor
{
    internal class DragSession
    {
        // ドラッグ開始時の各アイテムのStartTick（不変）
        public IReadOnlyDictionary<Guid, long> OriginalTicks { get; }

        // スナップ済みオフセット（フレームごとに更新）
        public long SnappedOffsetTick { get; private set; } = 0;

        // 表示用Tick（Build()に渡す）
        public IReadOnlyDictionary<Guid, long> DisplayTicks =>
            OriginalTicks.ToDictionary(kv => kv.Key, kv => kv.Value + SnappedOffsetTick);

        public DragSession(IEnumerable<ITimelineItem> items)
        {
            OriginalTicks = items.ToDictionary(i => i.Id, i => i.StartTick);
        }

        // オフセットのみ更新（スナップ計算はVM側でやって渡す）
        public void SetOffset(long snappedOffsetTick)
        {
            SnappedOffsetTick = snappedOffsetTick;
        }
    }
}
