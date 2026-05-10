using Avalonia;
using Avalonia.Controls;
using Microtone.Interfaces;
using Microtone.Models;
using Microtone.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microtone.Views.Services
{
    internal class DialogService : IDialogService
    {
        private readonly Window _owner;
        public DialogService(Window owner) { _owner = owner; }

        public async Task<List<Harmonograph>?> ShowChordInputDialog(Dimensions<int> offset1d)
        {
            var dialog = new ChordInputDialog(offset1d);
            return await dialog.ShowDialog<List<Harmonograph>?>(_owner);
        }
    }
}
