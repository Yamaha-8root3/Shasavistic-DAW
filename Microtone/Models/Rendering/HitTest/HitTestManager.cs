using Avalonia;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Microtone.Models.Rendering.HitTest
{
    public class HitTestManager
    {
        private bool isSorted = false;
        // 可視要素リスト（ZIndex降順ソート済み）
        private readonly List<HitInfo> _items = new();

        // 高速検索用
        private Dictionary<Guid, HitInfo> _hitInfoById = new();

        private readonly object _lock = new();

        public void Clear()
        {
            lock (_lock) { _items.Clear(); _hitInfoById.Clear(); }
        }

        // 登録・更新
        public void Register(HitInfo hitInfo)
        {
            lock (_lock) { _hitInfoById[hitInfo.TargetId] = hitInfo; _items.Add(hitInfo); isSorted = false; }
        }

        public void Remove(Guid id)
        {
            lock (_lock)
            {
                _items.Remove(_hitInfoById[id]);
                _hitInfoById.Remove(id);
            }
        }

        public void UpdateBounds(Guid id, SKRect newBounds)
        {
            lock (_lock)
            {
                if (_hitInfoById.TryGetValue(id, out var hit))
                {
                    hit.Bounds = newBounds;
                }
            }
        }

        // 可視リスト更新（ビューポート変更時）
        //public void UpdateVisibleList(SKRect viewport)
        //{
        //    _visibleItems = _hitInfoById.Values
        //        .Where(h => viewport.IntersectsWith(h.Bounds))
        //        .OrderByDescending(h => h.ZIndex)
        //        .ToList();
        //}

        private void SortItems()
        {
            lock (_lock)
            {
                _items.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex)); // 降順
            }
        }

        // 判定
        public HitInfo? HitTest(SKPoint point)
        {
            lock (_lock)
            {
                if (!isSorted) SortItems();
                foreach (var hit in _items)
                {
                    // 第1段階: 高速な矩形判定
                    if (!hit.Bounds.Contains(point)) continue;

                    // 第2段階: 詳細判定
                    if (hit.Path != null)
                    {
                        if (hit.Path.Contains(point.X, point.Y))
                        {
                            return hit;
                        }
                    }
                    else
                    {
                        // Pathがない = 矩形として判定
                        return hit;
                    }
                }
                return null;
            }
        }

        public HitInfo? GetHitInfo(Guid id)
        {
            lock (_lock) { return _hitInfoById.TryGetValue(id, out var hit) ? hit : null; }
        }
    }
}
