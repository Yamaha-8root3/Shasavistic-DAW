using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Interfaces.Editor
{
  internal interface ISelectionState
  {
    void Select(Guid  id, bool additive);
    void Deselect(Guid  id);
    void Clear();
    bool IsEmpty { get; }
  }
}
