using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Interfaces.Score
{
    public interface ITimelinePoint
    {
        Guid Id { get; }
        long StartTick { get; set; }
        double? TimeSeconds { get; }
        void UpdateTime(double timeSeconds);
    }
}
