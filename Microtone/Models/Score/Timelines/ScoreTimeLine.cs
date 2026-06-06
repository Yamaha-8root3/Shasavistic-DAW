using Microtone.Interfaces.Score;
using Microtone.Models.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microtone.Models.Score.Timelines
{
  public class ScoreTimeLine<T> : IScoreTimeline
                                      where T : ITimelinePoint
  {
    protected readonly Dictionary<Guid, T> _items = [];
    protected readonly SortedDictionary<long, Guid> _idByTick = [];

    public IReadOnlyList<T> Items => _items.Values.ToArray().AsReadOnly();
    public IReadOnlyDictionary<Guid, T> ItemsById => _items;
    public IReadOnlyDictionary<long, Guid> IdByTick => _idByTick;

    //IReadOnlyList<ITimelineItem> IScoreTimeline.Items => [.. Items];

    public void Add(T item)
    {
      if (_idByTick.ContainsKey(item.StartTick)) return;

      _items.Add(item.Id, item);
      _idByTick.Add(item.StartTick, item.Id);
      //_items.Add(item);
      //_items.Sort((a, b) => a.StartTick.CompareTo(b.StartTick));
    }
    
    public bool UpdateTick(Guid id, long newTick)
    {
      if (!_items.TryGetValue(id, out var item)) return false;
      _idByTick.Remove(item.StartTick);
      item.StartTick = newTick;
      _idByTick[newTick] = id;
      return true;
    }

    public bool Remove(Guid id)
    {
      _idByTick.Remove(_items[id].StartTick);
      return _items.Remove(id);
      //var removed = _items.RemoveAll(x => x.Id == id);
      //return removed;
    }

    public IReadOnlyList<T> QuerybyTime(double startSec, double endSec)
    {
      return [.. _items.Values.Where(x => x.TimeSeconds >= startSec && x.TimeSeconds <= endSec)];
    }

    public T? QuerybyTick(long tick)
    {
      if (_idByTick.TryGetValue(tick, out var id))
        return _items[id];
      else
        return default;
    }

    public T? QueryLatestByTick(long tick)
    {
      return _items.Values.Where(x => x.StartTick <= tick).OrderByDescending(x => x.StartTick).FirstOrDefault();
    }

    public void RecalculateTimes(TimeSignatureMap signatureMap)
    {
      foreach (var item in _items)
        item.Value.UpdateTime(signatureMap.TickToSeconds(item.Value.StartTick));
    }

  }
}
