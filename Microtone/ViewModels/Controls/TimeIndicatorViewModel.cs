using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Microtone.ViewModels.Controls
{
    public class TimeIndicatorViewModel : ReactiveObject
    {
        private long _currentTick;
        public long CurrentTick
        {
            get => _currentTick;
            set => this.RaiseAndSetIfChanged(ref _currentTick, value);
        }

        private bool _isMusicMode = true;
        public bool IsMusicMode
        {
            get => _isMusicMode;
            set => this.RaiseAndSetIfChanged(ref _isMusicMode, value);
        }
        public string ModeLabel => IsMusicMode ? "音楽" : "時間";

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => this.RaiseAndSetIfChanged(ref _isEditing, value);
        }

        private string _editText = "";
        public string EditText
        {
            get => _editText;
            set => this.RaiseAndSetIfChanged(ref _editText, value);
        }

        public string DisplayText => IsMusicMode
            ? TickToMusicString(CurrentTick)
            : TickToTimeString(CurrentTick);

        public ReactiveCommand<Unit, Unit> SetMusicModeCommand { get; }
        public ReactiveCommand<Unit, Unit> SetTimeModeCommand { get; }

        public TimeIndicatorViewModel()
        {
            SetMusicModeCommand = ReactiveCommand.Create(() => { IsMusicMode = true; });
            SetTimeModeCommand = ReactiveCommand.Create(() => { IsMusicMode = false; });

            // CurrentTickやIsMusicModeが変わったらDisplayTextも通知
            this.WhenAnyValue(x => x.CurrentTick, x => x.IsMusicMode)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(DisplayText));
                this.RaisePropertyChanged(nameof(ModeLabel));
            });
        }

        public void CommitEdit(string input)
        {
            var tick = IsMusicMode ? ParseMusicString(input) : ParseTimeString(input);
            if (tick.HasValue) CurrentTick = tick.Value;
        }

        private string TickToMusicString(long tick) => $"{tick}"; // 仮
        private string TickToTimeString(long tick) => $"{tick}";  // 仮
        private long? ParseMusicString(string s) => long.TryParse(s, out var v) ? v : null;
        private long? ParseTimeString(string s) => long.TryParse(s, out var v) ? v : null;
    }
}
