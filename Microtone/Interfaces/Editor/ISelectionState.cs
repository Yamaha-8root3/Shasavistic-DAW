using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Interfaces.Editor
{
  internal interface ISelectionState
  {
    void Select(object id, bool additive);
    void Clear();
    bool IsEmpty { get; }
  }
}
