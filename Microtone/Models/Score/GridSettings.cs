using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Models.Score
{
    internal record GridSettings
    {
        public int Division { get; set; } = 4;   // n分音符のn
        public bool SnapEnabled { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
    }
}
