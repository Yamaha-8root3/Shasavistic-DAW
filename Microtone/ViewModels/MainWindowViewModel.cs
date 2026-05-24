using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Microtone.Interfaces;
using Microtone.Models;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.HitTest;
using Microtone.Models.Rendering.SKRenderCommands;
using Microtone.Models.Score;
using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines.ScoreItems;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using Microtone.Services;
using Microtone.Services.Editor;
using Microtone.ViewModels.Controls;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Joins;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Microtone.Controls.DiagramTimeline;

namespace Microtone.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {

    SKRenderData _renderData = new();

    IDialogService _dialogService;
    public SKRenderData RenderData
    {
      get => _renderData;
      private set => SetProperty(ref _renderData, value);
    }
    private Guid? _selectedChordDiagramId;
    public Guid? SelectedChordDiagramId
    {
      get => _selectedChordDiagramId;
      private set => SetProperty(ref _selectedChordDiagramId, value);
    }
    private long _cursorTick = 0;
    public long CursorTick
    {
      get => _cursorTick;
      private set => UpdateCursorTick(value);
    }

    public TimeIndicatorViewModel TimeIndicatorVM { get; } = new();

    public ICommand OnScaleChangedCommand { get; }
    public ICommand OnPressedCommand { get; }
    public ICommand OnDragCommand { get; }
    public ICommand OnDragReleasedCommand { get; }
    public ICommand OnRightClickedCommand { get; }
    public ICommand PlaceChordDiagramCommand { get; }

    //テスト用
    private ScoreRenderTheme theme = new ScoreRenderTheme();
    private ScoreData score = new(1);
    private GridSettings _grid = new();
    private DragSession? _dragSession = null;
    private Point _rightClickWorldPos;

    public MainWindowViewModel(IDialogService dialogService)
    {
      _dialogService = dialogService;

      OnScaleChangedCommand = new RelayCommand<double>(OnScaleChanged);
      OnPressedCommand = new RelayCommand<PressedInfo?>(OnPressed);
      OnDragCommand = new RelayCommand<DragInfo>(OnDrag);
      OnDragReleasedCommand = new RelayCommand(OnDragReleased);
      OnRightClickedCommand = new RelayCommand<Point>(OnRightClicked);
      PlaceChordDiagramCommand = new RelayCommand(PlaceChordDiagram);

      PlayCommand = new RelayCommand(Play, () => !IsPlaying);
      StopCommand = new RelayCommand(Stop, () => IsPlaying);

      TimeIndicatorVM = new TimeIndicatorViewModel { TimeSignatureMap = score.TimeSignatureMap ,CurrentTick = CursorTick};
      TimeIndicatorVM.WhenAnyValue(x => x.CurrentTick)
        .Subscribe(tick => UpdateCursorTick(tick));
      //0tick BPM120 4/4
      score.TimeSignatureMap.Add(new(0, 120, 4, 4));
      //score.TimeSignatureMap.Add(new(480*4, 180, 4, 4));
      //0tick Tonic to <0D>
      score.BenchMarkMap.Add(new(
          -200,
          new InitialCenteredPitch(new([0])),
          new InitialCenteredPitch(new([0]))
      ));

      //0tick 2D↑ infinity
      score.ScorelineMap.Add(new(-200, [new(true, new([-1, 1]), false, true)]));

      score.DefiningChordMap.Add(new(new([0, 1]), new([0, 0, 0, 1])));


      var data = DiagramTimelineRenderDataBuilder.Build(score, theme, _grid);

      RenderData = data;
    }

    private void RebuildRenderData()
    {
      var data = DiagramTimelineRenderDataBuilder.Build(score, theme, _grid, SelectedChordDiagramId, _dragSession?.DisplayTicks);
      RenderData = data;
      // 差し替え後に現在のカーソルを再適用
      UpdateCursorTick(_cursorTick);
    }

    private void OnScaleChanged(double delta)
    {
      theme.PixelPerQuarter *= (float)(delta > 0 ? 1.1 : 0.9);
      RebuildRenderData();
    }
    private void OnPressed(PressedInfo? info)
    {
      if (info == null) return;
      if (info.Hit == null || info.Hit.Kind != HitKind.ChordDiagramBody)
      {
        // 空白クリック → 選択解除
        SelectedChordDiagramId = null;
        CursorTick = _grid.SnapEnabled ?
          GridSnapper.SnapTick((long)(info.WorldPos.X / theme.PixelPerQuarter * score.TimeSignatureMap.PPQ), score.TimeSignatureMap, _grid.Division, 100) :
          (long)(info.WorldPos.X / theme.PixelPerQuarter * score.TimeSignatureMap.PPQ);
      }
      else
      {
        SelectedChordDiagramId = info.Hit.TargetId;
        CursorTick = score.ScoreTimeLines
          .SelectMany(t => t.Items)
          .FirstOrDefault(i => i.Id == SelectedChordDiagramId)?.StartTick ?? CursorTick;
      }
      RebuildRenderData();
    }

    private void OnDrag(DragInfo? info)
    {
      if (SelectedChordDiagramId == null) return;
      if (info == null) return;

      if (_dragSession == null)
      {
        // 選択中の ChordDiagram を逆引き
        var cd = score.ScoreTimeLines
            .SelectMany(t => t.Items)
            //.OfType<ChordDiagram>()
            .FirstOrDefault(c => c.Id == SelectedChordDiagramId);
        if (cd == null) return;
        _dragSession = new DragSession([cd]);
      }

      // ピクセル → rawOffset(Tick)
      long rawOffset = (long)(info.DragDelta.X / theme.PixelPerQuarter * score.TimeSignatureMap.PPQ);


      // スナップはVM側で計算してSessionに渡す
      long snappedOffset;
      if (_grid.SnapEnabled)
      {
        long representativeTick = _dragSession.OriginalTicks.Values.Min() + rawOffset;
        long snapped = GridSnapper.SnapTick(representativeTick, score.TimeSignatureMap, _grid.Division, 100);
        snappedOffset = snapped - _dragSession.OriginalTicks.Values.Min();
      }
      else
      {
        snappedOffset = rawOffset;
      }
      _dragSession.SetOffset(snappedOffset);

      RebuildRenderData();
    }

    private void OnDragReleased()
    {
      if (_dragSession == null) return;
      long leftLimit = score.BenchMarkMap.Items[0].StartTick;

      foreach (var (id, originalTick) in _dragSession.OriginalTicks)
      {
        var item = score.ScoreTimeLines
            .SelectMany(t => t.Items)
            .FirstOrDefault(i => i.Id == id);
        if (item != null)
          item.StartTick = Math.Max(leftLimit, originalTick + _dragSession.SnappedOffsetTick);
      }

      _dragSession = null;
      RebuildRenderData();
    }



    private void OnRightClicked(Point worldPos)
    {
      _rightClickWorldPos = worldPos;
      if (SelectedChordDiagramId != null)
      {

      }
    }

    private async void PlaceChordDiagram()
    {
      var tick = CursorTick;

      var result = await _dialogService.ShowChordInputDialog(score.Dimension1DOffset);
      if (result is null) return;

      var cd = new ChordDiagram()
      {
        StartTick = (long)tick,
        LengthTick = 240,
      };
      var baseFormula = result[0].ToOvertoneFormula(score.Dimension1DOffset);
      var basePl = cd.AddPitchLine(baseFormula)!;
      basePl.IsBase = true;
      int baseplid = basePl.Id;

      foreach (var item in result)
      {
        var formula = item.ToOvertoneFormula(score.Dimension1DOffset);
        if (formula.Ratio.Value == baseFormula.Ratio.Value) continue; // ベース音はスキップ
        var plid = cd.AddPitchLine(formula)?.Id;
        if (plid is null) continue;
        cd.AddDimensionlineChain(baseplid, (int)plid, score.Dimension1DOffset);
      }
      score.ScoreTimeLines[0].Add(cd);
      RebuildRenderData();
    }

    private void UpdateCursorTick(long tick)
    {
      if (_cursorTick != tick)
      {
        _cursorTick = tick;
        TimeIndicatorVM.CurrentTick = tick;
      }
      SKRenderCommand cmd = DiagramTimelineRenderDataBuilder.BuildCursor(score, theme, tick);
      RenderData.SetCursorCommand(cmd); // 常に現在のRenderDataインスタンスに対して実行
    }


    private CancellationTokenSource? _playCts;
    public bool IsPlaying => _playCts != null;

    public ICommand PlayCommand { get; }
    public ICommand StopCommand { get; }

    private void Play()
    {
      if (IsPlaying) return;
      _playCts = new CancellationTokenSource();
      var token = _playCts.Token;
      var startTick = _cursorTick;
      var startSeconds = score.TimeSignatureMap.TickToSeconds(startTick); // 開始点の絶対秒
      var startTime = DateTime.UtcNow;

      Task.Run(async () =>
      {
        while (!token.IsCancellationRequested)
        {
          await Task.Delay(16, token).ContinueWith(_ => { });
          var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
          var tick = score.TimeSignatureMap.SecondsToTick(startSeconds + elapsed);
          Avalonia.Threading.Dispatcher.UIThread.Post(() => CursorTick = tick);
        }
      }, token);

      OnPropertyChanged(nameof(IsPlaying));

      ((RelayCommand)PlayCommand).NotifyCanExecuteChanged();
      ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
    }

    private void Stop()
    {
      _playCts?.Cancel();
      _playCts = null;
      OnPropertyChanged(nameof(IsPlaying));

      ((RelayCommand)PlayCommand).NotifyCanExecuteChanged();
      ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
    }
  }
}
