using Microtone.Models.Score.Timelines;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;

namespace Microtone.ViewModels.Controls
{
    public class TimeIndicatorViewModel : ReactiveObject
    {

    private TimeSignatureMap? _timeSignatureMap;
    public TimeSignatureMap? TimeSignatureMap
    {
      get => _timeSignatureMap;
      set => this.RaiseAndSetIfChanged(ref _timeSignatureMap, value);
    }

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
            });
        }

    private string TickToMusicString(long tick)
    {
      if (_timeSignatureMap == null) return $"{tick}";
      var (bar, beat, rem) = _timeSignatureMap.TickToBarBeat(tick); // Math.Abs削除
      if (tick < 0)
        return $"-{bar} : {beat} : {rem:D3}";
      return $"{bar} : {beat} : {rem:D3}";
    }

    private string TickToTimeString(long tick)
    {
      if (_timeSignatureMap == null) return $"{tick}";
      double sec = _timeSignatureMap.TickToSeconds(Math.Abs(tick));
      var ts = TimeSpan.FromSeconds(sec);
      if (tick < 0)
        return $"-{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
      return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    public void CommitEdit(string input)
    {
      if (IsMusicMode)
      {
        // "小節 : 拍 : tick" or 整数
        var parts = input.Split(':');
        if (parts.Length == 3 &&
            int.TryParse(parts[0].Trim(), out int bar) &&
            int.TryParse(parts[1].Trim(), out int beat) &&
            int.TryParse(parts[2].Trim(), out int rem) &&
            _timeSignatureMap != null)
        {
          CurrentTick = _timeSignatureMap.BarBeatToTick(bar, beat, rem);
          return;
        }
      }
      var tick = ParseTimeString(input);
      if (tick.HasValue) CurrentTick = tick.Value;
    }

    // ParseTimeStringは秒入力("mm:ss.ms")対応
    private long? ParseTimeString(string s)
    {
      if (long.TryParse(s, out var v)) return v;
      // mm:ss.ms
      if (TimeSpan.TryParseExact(s, @"mm\:ss\.fff", null, out var ts) && _timeSignatureMap != null)
      {
        // 秒→Tick (簡易: 最初のBPMのみ)
        double bpm = _timeSignatureMap.Items.FirstOrDefault()?.Bpm ?? 120;
        return (long)(ts.TotalSeconds * _timeSignatureMap.PPQ * bpm / 60.0);
      }
      return null;
    }
  }
}
