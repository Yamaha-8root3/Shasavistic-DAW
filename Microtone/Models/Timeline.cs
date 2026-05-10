using Avalonia.Remote.Protocol.Designer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microtone.Models
{
    public class TimedValue<T>
    {
        public float Start { get; init; }
        public T? Value { get; init; }
    }

    public readonly struct TimeRange
    {
        public float Start { get; init; }
        public float? End { get; init; } // null = 無限

        public bool Contains(double t)
        {
            if (t < Start) return false;
            if (End.HasValue && t >= End.Value) return false;
            return true;
        }
    }

    public sealed class TimedRange<T>
    {
        public TimeRange Range { get; init; }
        public float Start => Range.Start;
        public float? End => Range.End;
        public T? Value { get; init; }
    }

    public class Timeline<T>()
    {
        private readonly List<TimedValue<T>> _items = [];
        public IReadOnlyList<TimedValue<T>> Items => _items;


        public void Add(float start, T value)
        {
            _items.Add(new TimedValue<T> { Start = start, Value = value });
            _items.Sort((a, b) => a.Start.CompareTo(b.Start));
        }
        public void RemoveAt(float x)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (x >= _items[i].Start)
                {
                    _items.RemoveAt(i);
                    return;
                }
            }
        }
        public T? GetValueAt(float x)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (x >= _items[i].Start)
                    return _items[i].Value;
            }
            return default;
        }

        public TimeRange GetRange(int index)
        {
            var start = _items[index].Start;
            float? end = index + 1 < _items.Count
              ? _items[index + 1].Start
              : null;

            return new TimeRange { Start = start, End = end };
        }

        public IEnumerable<TimedRange<T>> Query(float viewStart, float viewEnd)
        {

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var itemEnd = i == _items.Count - 1 ? float.PositiveInfinity : _items[i + 1].Start;
                if (item.Start < viewEnd && itemEnd > viewStart)
                    yield return new TimedRange<T> { Value = item.Value, Range = new TimeRange { Start = item.Start, End = itemEnd} };
            }
        }
    }
}
