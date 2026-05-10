using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.ViewModels.Dialogs
{
    internal class ChordInputDialogViewModel : ViewModelBase
    {
        private string _inputText = "";
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }
    }
}
