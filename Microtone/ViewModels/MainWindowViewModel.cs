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

    private ScoreSession _session = new();
    private ScoreData score => _session.Score;
    private ScoreRenderTheme theme => _session.Theme;
    private GridSettings _grid => _session.Grid;

    private ScoreEditor _editor;

    private DragSession? _dragSession = null;
    private Point _rightClickWorldPos;

    public MainWindowViewModel(IDialogService dialogService)
    {
      _editor = new ScoreEditor(_session);


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


      var data = DiagramTimelineRenderDataBuilder.Build(_session);

      RenderData = data;
    }

    private void RebuildRenderData()
    {
      RenderData = _session.BuildRenderData(SelectedChordDiagramId, _dragSession?.DisplayTicks);
      // 差し替え後に現在のカーソルを再適用
      UpdateCursorTick(_cursorTick);
    }

    private void OnScaleChanged(double delta)
    {
      _session.PixelPerQuarter *= (float)(delta > 0 ? 1.1 : 0.9);
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
          GridSnapper.SnapTick((long)(info.WorldPos.X / _session.PixelPerQuarter * score.TimeSignatureMap.PPQ), score.TimeSignatureMap, _grid.Division, 100) :
          (long)(info.WorldPos.X / _session.PixelPerQuarter * score.TimeSignatureMap.PPQ);
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
      if (SelectedChordDiagramId == null || info == null) return;
      _dragSession ??= new DragSession([_editor.FindItem(SelectedChordDiagramId.Value)!]);

      long rawOffset = _editor.PixelToTick(info.DragDelta.X); // ← Editorへ委譲
      long snappedOffset = _editor.SnapDragOffset(_dragSession.OriginalTicks.Values.Min() , rawOffset);
      _dragSession.SetOffset(snappedOffset);
      RebuildRenderData();
    }

    private void OnDragReleased()
    {
      if (_dragSession == null) return;
      _editor.CommitDrag(_dragSession);
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
      var result = await _dialogService.ShowChordInputDialog(_session.Score.Dimension1DOffset);
      if (result is null) return;
      _editor.PlaceChordDiagram(CursorTick, result);
      RebuildRenderData();
    }

    private void UpdateCursorTick(long tick)
    {
      if (_cursorTick != tick)
      {
        _cursorTick = tick;
        TimeIndicatorVM.CurrentTick = tick;
      }
      SKRenderCommand cmd = DiagramTimelineRenderDataBuilder.BuildCursor(_session, tick);
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
