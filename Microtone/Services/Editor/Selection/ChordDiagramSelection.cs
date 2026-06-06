using Microtone.Interfaces.Editor;
using System;
using System.Collections.Generic;

namespace Microtone.Services.Editor.Selection
{
  internal class ChordDiagramSelection : ISelectionState
  {
    public HashSet<Guid> Ids { get; } = [];
    public bool IsEmpty => Ids.Count == 0;

    public ChordDiagramSelection() { }
    public ChordDiagramSelection(Guid id)
    {
      Ids.Add(id);
    }

    public void Select(Guid id, bool additive)
    {
      if (!additive) Clear();
      Ids.Add(id);
    }

    public void Deselect(Guid id)
    {
      Ids.Remove(id);
    }

    public void Clear() => Ids.Clear();
    public bool Contains(object id) => Ids.Contains((Guid)id);
  }
}
