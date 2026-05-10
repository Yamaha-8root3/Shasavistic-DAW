using Avalonia;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models
{
    internal record DragInfo
    {
        public Point DragStart;
        public Point DragEnd;
        public Point DragDelta => DragEnd - DragStart;
        //イベント間の一瞬の移動量 全体の移動量ではない
        public Point Delta;
    }
}
