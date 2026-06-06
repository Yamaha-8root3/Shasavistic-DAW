using CommunityToolkit.Mvvm.Input;
using Microtone.Models;
using Microtone.Models.Score.Timelines.ScoreItems.PitchLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microtone.Services.Editor.Selection;

namespace Microtone.ViewModels.Flyout
{
    public class PitchlineFlyoutViewModel : ViewModelBase
    {
        // private readonly List<(ChordDiagram Cd, Guid PlId)> _targets;
        private IEnumerable<(ChordDiagram, Guid)> Targets =>
            _selection.Ids
                .Where(plId => _parentCd.Pitchlines.ContainsKey(plId))
                .Select(plId => (_parentCd, plId))
                .ToList();
        private readonly PitchlineSelection _selection;
        private readonly ChordDiagram _parentCd;
        private readonly Dimensions<int> _dimension1DOffset;
        private readonly Action _onChanged;
        
        public bool Isroot => Targets.All(t => t.Item1.Pitchlines[t.Item2].IsRoot);
        public bool IsSinglePitchline => Targets.Count() == 1;
        public bool IsBase => IsSinglePitchline && _parentCd.GetBase()?.Id == Targets.First().Item2;
        
        public bool? IsOmitted
        {
            get
            {
                var vals = Targets
                    .Select(t => t.Item1.Pitchlines.TryGetValue(t.Item2, out var pl) && pl.IsIntermediate)
                    .Distinct()
                    .ToList();
                return vals.Count == 1 ? vals[0] : null;
            }
        }

        public ICommand ToggleOmitCommand { get; }
        public ICommand AddNoteCommand { get; }  // parameter: (int dim, int step)
        public ICommand RemoveNoteCommand { get; }
        public ICommand SetBaseCommand { get; }

        public PitchlineFlyoutViewModel(
            PitchlineSelection selection,
            ChordDiagram parentCd,
            Dimensions<int> dimension1DOffset,
            Action onChanged)
        {
            _selection = selection;
            _parentCd = parentCd;
            _dimension1DOffset = dimension1DOffset;
            _onChanged = onChanged;

            ToggleOmitCommand = new RelayCommand(ToggleOmit);
            AddNoteCommand = new RelayCommand<(int, int)>(p => AddNote(p.Item1, p.Item2));
            RemoveNoteCommand = new RelayCommand(RemoveNote);
            SetBaseCommand = new RelayCommand(SetBase);
        }

        private void ToggleOmit()
        {
            // 混在 or 全false → true、全true → false
            bool newVal = IsOmitted != true;
            foreach (var (cd, plId) in Targets)
                if (cd.Pitchlines.TryGetValue(plId, out var pl))
                    pl.IsIntermediate = newVal;
            OnPropertyChanged(nameof(IsOmitted));
            _onChanged();
        }

        private void AddNote(int dimension, int step)
        {
            foreach (var (cd, plId) in Targets.ToList())
            {
                if (!cd.Pitchlines.TryGetValue(plId, out var pl)) continue;

                // 新しい音のFormulaを計算
                var newFormula = pl.Formula.Clone();
                newFormula.Add(dimension,step);
                // 1次元補正
                newFormula.Add(1,_dimension1DOffset[dimension] * step);

                var newPl = cd.AddPitchLine(newFormula);
                cd.AddDimensionlineChain(plId, newPl.Id, _dimension1DOffset);
                _selection.Deselect(plId);
                _selection.Select(newPl.Id,true);
            }
            _onChanged();
        }

        private void RemoveNote()
        {
            // var newtargets = new List<(ChordDiagram, Guid)>();
            var targets = Targets.ToList();
            _selection.Clear();
            foreach (var (cd, pid) in targets)
            {
                var related = cd.GetRelatedPitchLines(pid);
                cd.RemovePitchLine(pid);
                var remaining = cd.Pitchlines.Values.Where(related.Contains).ToList();
                if ( remaining.Count > 0)
                {
                    // newtargets.Add((cd, remaining.First().Id));
                    _selection.Select(remaining.First().Id,true);
                }
            }
            _onChanged();
        }

        private void SetBase()
        {
            _parentCd.SetBase(Targets.First().Item2);
            OnPropertyChanged(nameof(IsBase));
            _onChanged();
        }
    }
}