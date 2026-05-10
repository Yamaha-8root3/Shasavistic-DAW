using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Microtone.Interfaces;
using Microtone.Models;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.HitTest;
using Microtone.Models.Score;
using Microtone.Models.Score.Pitch;
using Microtone.Models.Score.Timelines.ScoreItems;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using Microtone.Services;
using Microtone.Services.Editor;
using Microtone.ViewModels.Controls;
using System;
using System.Linq;
using System.Windows.Input;

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
            OnPressedCommand = new RelayCommand<HitInfo?>(OnPressed);
            OnDragCommand = new RelayCommand<DragInfo>(OnDrag);
            OnDragReleasedCommand = new RelayCommand(OnDragReleased);
            OnRightClickedCommand = new RelayCommand<Point>(OnRightClicked);
            PlaceChordDiagramCommand = new RelayCommand(PlaceChordDiagram);


            //0tick BPM120 4/4
            score.TimeSignatureMap.Add(new(0, 120, 4, 4));
            //0tick Tonic to <0D>
            score.BenchMarkMap.Add(new(
                -200,
                new InitialCenteredPitch(new([0])),
                new InitialCenteredPitch(new([0]))
            ));

            //0tick 2D↑ infinity
            score.ScorelineMap.Add(new(-200, [new(true, new([-1, 1]), false, true)]));

            score.DefiningChordMap.Add(new(new([0, 1]), new([0, 0, 0, 1])));

            //var pl = new ChordDiagram()
            //{
            //    StartTick = 0,
            //    LengthTick = 240,
            //};
            //var a = pl.AddPitchLine(new Harmonograph([0,-1]).ToOvertoneFormula(score.Dimension1DOffset));
            //a.IsBase = true;
            ////var b = pl.AddPitchLine(new Harmonograph([0,1]).ToOvertoneFormula(score.Dimension1DOffset));
            //var c = pl.AddPitchLine(new Harmonograph([0,0,1]).ToOvertoneFormula(score.Dimension1DOffset));
            ////pl.AddDimensionlineChain(a.Id, b.Id, score.Dimension1DOffset);
            //pl.AddDimensionlineChain(a.Id, c.Id, score.Dimension1DOffset);
            //score.ScoreTimeLines[0].Add(pl);


            //var f = new DetailedFunctograph()
            //{
            //    StartTick = 500,
            //    LengthTick = 200,
            //    HostUp = [NoteState.Default, NoteState.Omit],
            //    GuestDown = [NoteState.Omit],
            //};
            //score.ScoreTimeLines[0].Add(f);
            //_grid.Division = 4;



            var data = DiagramTimelineRenderDataBuilder.Build(score, theme, _grid);

            RenderData = data;
        }

        private void RebuildRenderData()
        {
            var data = DiagramTimelineRenderDataBuilder.Build(score, theme, _grid, SelectedChordDiagramId, _dragSession?.DisplayTicks);
            RenderData = data;
        }

        private void OnScaleChanged(double delta)
        {
            theme.PixelPerQuarter *= (float)(delta > 0 ? 1.1 : 0.9);
            RebuildRenderData();
        }
        private void OnPressed(HitInfo? hit)
        {
            if (hit == null || hit.Kind != HitKind.ChordDiagramBody)
            {
                // 空白クリック → 選択解除
                SelectedChordDiagramId = null;
            }
            else
            {
                SelectedChordDiagramId = hit.TargetId;
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
        }

        private async void PlaceChordDiagram()
        {
            //tick / PPQ * pixelPerQuarter = pixel
            var tick = _rightClickWorldPos.X / theme.PixelPerQuarter * score.TimeSignatureMap.PPQ;

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
    }
}
