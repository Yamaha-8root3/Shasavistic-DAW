using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Microtone.Interfaces;
using Microtone.Interfaces.Editor;
using Microtone.Models;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.HitTest;
using Microtone.Models.Score;
using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using Microtone.Services;
using Microtone.Services.Editor;
using Microtone.Services.Editor.Selection;
using Microtone.ViewModels.Controls;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Microtone.Controls;
using Microtone.ViewModels.Flyout;
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
    private long _cursorTick = 0;
    public long CursorTick
    {
      get => _cursorTick;
      private set => UpdateCursorTick(value);
    }

    public bool IsFlyoutOpen
    {
      get;
      private set => SetProperty(ref field, value);
    } = false;

    public double FlyoutX
    {
      get;
      private set => SetProperty(ref field, value);
    }

    public double FlyoutY
    {
      get;
      private set => SetProperty(ref field, value);
    }

    public ViewModelBase? FlyoutContent
    {
      get;
      private set => SetProperty(ref field, value);
    }

    public SKPoint TimelineScale { get; set; }
    public Point TimelineOffset { get; set; }

    private Rect? _selectedHitBoundScreen;
    public Rect? SelectedHitBoundScreen
    {
      get => _selectedHitBoundScreen;
      set
      {
        SetProperty(ref _selectedHitBoundScreen, value);
        UpdateFlyoutPosition();
      }
    }

    public Guid? SelectedItemId
    {
      get;
      private set => SetProperty(ref field, value);
    }

    public TimeIndicatorViewModel TimeIndicatorVM { get; } = new();

    public ICommand OnScaleChangedCommand { get; }
    public ICommand OnPressedCommand { get; }
    public ICommand OnDragCommand { get; }
    public ICommand OnDragReleasedCommand { get; }
    public ICommand OnClickedCommand { get; }
    public ICommand PlaceChordDiagramCommand { get; }
    public ICommand PlaceEmptyChordDiagramCommand { get; }

    private ScoreSession _session = new();
    private ScoreData score => _session.Score;
    private GridSettings _grid => _session.Grid;

    private ScoreEditor _editor;

    private DragSession? _dragSession = null;

    ISelectionState? _selection;
    private object? _pendingSelection = null;
    
    public MainWindowViewModel(IDialogService dialogService)
    {
      _editor = new ScoreEditor(_session);


      _dialogService = dialogService;
      OnPressedCommand = new RelayCommand<ClickedInfo?>(OnPressed);
      OnScaleChangedCommand = new RelayCommand<double>(OnScaleChanged);
      OnDragCommand = new RelayCommand<DragInfo>(OnDrag);
      OnDragReleasedCommand = new RelayCommand(OnDragReleased);
      OnClickedCommand = new RelayCommand<ClickedInfo?>(OnClicked);
      PlaceChordDiagramCommand = new RelayCommand(PlaceChordDiagram);
      PlaceEmptyChordDiagramCommand = new RelayCommand(PlaceEmptyChordDiagram);
      _editor.TimelineObjectRemoved += OnObjectRemoved;

      PlayCommand = new RelayCommand(Play, () => !IsPlaying);
      StopCommand = new RelayCommand(Stop, () => IsPlaying);

      TimeIndicatorVM = new TimeIndicatorViewModel { TimeSignatureMap = score.TimeSignatureMap, CurrentTick = CursorTick };
      TimeIndicatorVM.WhenAnyValue(x => x.CurrentTick)
        .Subscribe(tick => UpdateCursorTick(tick));
      score.BenchMarkMap.Add(new(
          -200,
          new InitialCenteredPitch(new([0])),
          new InitialCenteredPitch(new([0]))
      ));

      //0tick 2D↑ infinity
      score.ScorelineMap.Add(new(-200, [new(true, new([-1, 1]), false, true)]));

      score.DefiningChordMap.Add(new(new([0, 1]), new([0, 0, 0, 1])));

      var cd = new ChordDiagram { StartTick = 0, LengthTick = 240 };
      var a = cd.AddPitchLine(new Harmonograph([0]).ToOvertoneFormula(score.Dimension1DOffset));
      var b = cd.AddPitchLine(new Harmonograph([1, -1, 0, 1]).ToOvertoneFormula(score.Dimension1DOffset));
      cd.AddDimensionlineChain(a.Id, b.Id, score.Dimension1DOffset);
      // score.ScoreTimeLines[0].Add(cd);
      _editor.RegistorScoreTimelineObject(cd);
      var data = DiagramTimelineRenderDataBuilder.Build(_session);


      RenderData = data;
    }

    private void RebuildRenderData()
    {
      RenderData = _session.BuildRenderData(_selection, _dragSession?.DisplayTicks);
      // 差し替え後に現在のカーソルを再適用
      UpdateCursorTick(_cursorTick);
    }

    private void OnScaleChanged(double delta)
    {
      _session.PixelPerQuarter *= (float)(delta > 0 ? 1.1 : 0.9);
      RebuildRenderData();
    }
    private void OnPressed(ClickedInfo? info)
    {
      if (info == null) return;
      if (info.Hit == null || info.Hit.Kind == HitKind.None)
      {
        // 空白クリック → 選択解除
        _selection = null;
        IsFlyoutOpen = false;
        CursorTick = _grid.SnapEnabled ?
          GridSnapper.SnapTick((long)(info.WorldPos.X / _session.PixelPerQuarter * score.TimeSignatureMap.PPQ), score.TimeSignatureMap, _grid.Division, 100) :
          (long)(info.WorldPos.X / _session.PixelPerQuarter * score.TimeSignatureMap.PPQ);
        RebuildRenderData();
        return;
      }
      var additive = info.Modifiers == Avalonia.Input.KeyModifiers.Control;
      switch (info.Hit.Kind)
      {
          case HitKind.ChordDiagramBody:
          if (_selection is not ChordDiagramSelection cdSel)
            _selection = cdSel = new ChordDiagramSelection();

          _pendingSelection = cdSel.Ids.Contains(info.Hit.TargetId) && !additive
              ? info.Hit.TargetId
              : null;

          if (_pendingSelection == null)
            _selection.Select(info.Hit.TargetId, additive);

          IsFlyoutOpen = false;
          SelectedItemId = info.Hit.TargetId;
          CursorTick = _editor.FindItem(info.Hit.TargetId)?.StartTick ?? CursorTick;

          break;

        case HitKind.Pitchline:
          if (info.Hit.Attributes.TryGetValue("ChordDiagramId", out var cdid) && cdid is Guid)
          {
             if (_selection is not PitchlineSelection selection || selection.ParentChordDiagramId != (Guid)cdid)
                _selection = new PitchlineSelection((Guid)cdid);
             _selection.Select(info.Hit.TargetId, additive);
             SelectedItemId = info.Hit.TargetId;
          }
          FlyoutContent = BuildPitchlineFlyout();
          IsFlyoutOpen = true;
          UpdateFlyoutPosition();
          break;
      }
      RebuildRenderData();
    }

    private void OnDrag(DragInfo? info)
    {
      if (info is null || _selection is null) return;
      switch (_selection)
      {
        case ChordDiagramSelection cds:
          if (cds.IsEmpty) break;
          _dragSession ??= new DragSession(
            cds.Ids.Select(id => _editor.FindItem(id)!).Where(x => x != null));
          break;
      }
      if (_dragSession == null) return;
      long rawOffset = _editor.PixelToTick(info.DragDelta.X); // ← Editorへ委譲
      long snappedOffset = _editor.SnapDragOffset(_dragSession.OriginalTicks.Values.Min(), rawOffset);
      _dragSession.SetOffset(snappedOffset);
      RebuildRenderData();
    }

    private void OnDragReleased()
    {
      if (_dragSession != null)
      {
        _editor.CommitDrag(_dragSession);
        _dragSession = null;
      }
      else if (_pendingSelection != null)
      {
        switch (_selection)
        {
          case ChordDiagramSelection cdSel when _pendingSelection is Guid id && cdSel.Ids.Contains(id):
            _selection.Select(id, false);
            break;
        }
      }
      _pendingSelection = null;
      RebuildRenderData();
    }

    private void OnClicked(ClickedInfo? info)
    {
      if (info == null) return;
      switch (info.Button)
      {
        case Avalonia.Input.MouseButton.Left when info.Hit == null || info.Hit.Kind == HitKind.None:
          // IsFlyoutOpen = false;
          break;
        case Avalonia.Input.MouseButton.Left:
          // IsFlyoutOpen = _selection is PitchlineSelection;
          // UpdateFlyoutPosition(); 
          break;
        case Avalonia.Input.MouseButton.Right:
          break;
      }
    }

    private async void PlaceChordDiagram()
    {
      var result = await _dialogService.ShowChordInputDialog(_session.Score.Dimension1DOffset);
      if (result is null) return;
      var cd = _editor.PlaceChordDiagram(CursorTick, result);
      _selection = new ChordDiagramSelection(cd.Id);
      RebuildRenderData();
    }

    private void PlaceEmptyChordDiagram()
    {
      var cd = new ChordDiagram { StartTick = CursorTick, LengthTick = 240 };
      cd.AddPitchLine(new Harmonograph([0]).ToOvertoneFormula(score.Dimension1DOffset)).IsBase = true;
      _editor.RegistorScoreTimelineObject(cd);
      _selection = new ChordDiagramSelection(cd.Id);
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

    private void UpdateFlyoutPosition()
    {
      if (_selectedHitBoundScreen == null || !IsFlyoutOpen) return;
      FlyoutX = _selectedHitBoundScreen.Value.Right;
      FlyoutY = (_selectedHitBoundScreen.Value.Top + _selectedHitBoundScreen.Value.Bottom) / 2;
    }
    
    private PitchlineFlyoutViewModel? BuildPitchlineFlyout()
    {
      if (_selection is not PitchlineSelection pls || pls.IsEmpty) return null;
      if (_editor.FindItem(pls.ParentChordDiagramId) is not ChordDiagram cd) return null;
      return new PitchlineFlyoutViewModel(pls, cd, score.Dimension1DOffset, RebuildRenderData);
    }

    private void OnObjectRemoved(Guid id)
    {
      switch (_selection) 
      {
        case ChordDiagramSelection cds when cds.Ids.Contains(id):
          _selection = null;
          break;
        case PitchlineSelection pls when pls.ParentChordDiagramId == id:
          IsFlyoutOpen = false;
          _selection = null;
          break;
      }
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
