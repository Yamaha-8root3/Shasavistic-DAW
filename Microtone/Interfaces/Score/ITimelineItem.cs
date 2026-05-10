using Microtone.Models.Enums;
using Microtone.Models.Score;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Interfaces.Score
{

    public interface ITimelineItem : ITimelinePoint
    {
        //Guid Id { get; }
        //long StartTick { get; set; }
        long LengthTick { get; set; }
        LengthKind LengthKind { get; set; }
        bool Isresolved { get; set; }
        //double? TimeSeconds { get; } // キャッシュ
        //void UpdateTime(double timeseconds);
    }
}
