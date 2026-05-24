using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Rendering
{
  public class SKRenderData
  {
    public SKPaint Background { get; set; } = new();
    public List<SKRenderCommand> Commands { get; init; } = [];

    private readonly object _lock = new();

    public event Action? CursorChanged;
    public void SortCommandsByZIndex()
    {
      Commands.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
    }

    // カーソルコマンドのGuid（固定）
    private static readonly Guid CursorSlotId = Guid.NewGuid();
    private int _cursorCommandIndex = -1;

    public void SetCursorCommand(SKRenderCommand? cmd)
    {
      lock (_lock)
      {
        if (_cursorCommandIndex >= 0 && _cursorCommandIndex < Commands.Count)
          Commands.RemoveAt(_cursorCommandIndex);

        if (cmd == null) { _cursorCommandIndex = -1; return; }

        // ZIndexの位置に挿入（ソート済み前提）
        int i = Commands.BinarySearch(cmd, Comparer<SKRenderCommand>.Create(
            (a, b) => a.ZIndex.CompareTo(b.ZIndex)));
        int insertAt = i >= 0 ? i : ~i;
        Commands.Insert(insertAt, cmd);
        _cursorCommandIndex = insertAt;

      }
      CursorChanged?.Invoke();
    }
  }
}
