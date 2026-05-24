using Microtone.Interfaces.Editor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services.Editor.Selection
{
  internal class ChordDiagramSelection : ISelectionState
  {
    public HashSet<Guid> Ids { get; } = [];
    public bool IsEmpty => Ids.Count == 0;

    public void Select(object id, bool additive)
    {
      if (!additive) Clear();
      Ids.Add((Guid)id);
    }
    public void Clear() => Ids.Clear();
    public bool Contains(object id) => Ids.Contains((Guid)id);
  }
}
