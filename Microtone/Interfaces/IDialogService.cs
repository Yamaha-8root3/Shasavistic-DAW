using Avalonia;
using Microtone.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microtone.Interfaces
{
    public interface IDialogService
    {
        Task<List<Harmonograph>?> ShowChordInputDialog(Dimensions<int> offset1d);
    }
}
