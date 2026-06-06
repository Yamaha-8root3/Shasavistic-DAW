using Microtone.Interfaces.Editor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services.Editor.Selection
{
  public class PitchlineSelection : ISelectionState
  {
    public Guid ParentChordDiagramId { get; private set; }
    public HashSet<Guid> Ids { get; } = [];
    public bool IsEmpty => Ids.Count == 0;

    public PitchlineSelection(Guid parentCdId)
    {
      ParentChordDiagramId = parentCdId;
    }
    public void Select( Guid pitchlineId, bool additive)
    {
      if (!additive) Clear();
      Ids.Add(pitchlineId);
    }
    public void Deselect(Guid pitchlineId) => Ids.Remove(pitchlineId);

    public void Clear()
    {
      Ids.Clear();
    }
    public bool Contains(Guid pitchlineId)
        =>Ids.Contains(pitchlineId);
  }
}
