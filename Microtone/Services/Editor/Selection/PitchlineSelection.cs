using Microtone.Interfaces.Editor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services.Editor.Selection
{
  internal class PitchlineSelection : ISelectionState
  {
    public Guid ParentChordDiagramId { get; private set; }
    public HashSet<int> Ids { get; } = [];
    public bool IsEmpty => Ids.Count == 0;

    public void Select(Guid parentCdId, int pitchlineId, bool additive)
    {
      if (!additive || ParentChordDiagramId != parentCdId)
      {
        Clear();
        ParentChordDiagramId = parentCdId;
      }
      Ids.Add(pitchlineId);
    }
    public void Select(object id, bool additive)
    {
      if (id is not (Guid cdId, int plId)) return;
      Select(cdId, plId, additive);
    }

    public void Clear()
    {
      Ids.Clear();
      ParentChordDiagramId = default;
    }
    public bool Contains(Guid parentCdId, int pitchlineId)
        => ParentChordDiagramId == parentCdId && Ids.Contains(pitchlineId);
  }
}
