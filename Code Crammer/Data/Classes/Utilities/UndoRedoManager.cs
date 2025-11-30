#nullable enable

#nullable enable

using Code_Crammer.Data.Classes.Models;

namespace Code_Crammer.Data.Classes.Utilities
{
    public class UndoRedoManager
    {
        private Stack<AppState> _undoStack = new Stack<AppState>();
        private Stack<AppState> _redoStack = new Stack<AppState>();
        private const int MAX_UNDO_STEPS = 20;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void PushState(AppState currentState)
        {
            var stateCopy = CloneState(currentState);

            if (_undoStack.Count > 0)
            {
                var last = _undoStack.Peek();
                if (AreStatesEqual(last, stateCopy)) return;
            }

            _undoStack.Push(stateCopy);

            if (_undoStack.Count > MAX_UNDO_STEPS)
            {
                var limitedList = _undoStack.Take(MAX_UNDO_STEPS).Reverse().ToList();
                _undoStack = new Stack<AppState>(limitedList);
            }

            _redoStack.Clear();
        }

        public AppState? Undo(AppState currentState)
        {
            if (_undoStack.Count == 0) return null;

            var stateToRestore = _undoStack.Pop();
            _redoStack.Push(CloneState(currentState));

            return stateToRestore;
        }

        public AppState? Redo(AppState currentState)
        {
            if (_redoStack.Count == 0) return null;

            var stateToRestore = _redoStack.Pop();
            _undoStack.Push(CloneState(currentState));

            return stateToRestore;
        }

        private AppState CloneState(AppState source)
        {
            if (source == null) return new AppState();

            return new AppState
            {
                ActionDescription = source.ActionDescription,
                CheckedFiles = source.CheckedFiles != null
                    ? new HashSet<string>(source.CheckedFiles, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase),

                Options = source.Options != null
                    ? new Dictionary<string, bool>(source.Options)
                    : new Dictionary<string, bool>()
            };
        }

        private bool AreStatesEqual(AppState a, AppState b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;

            int aCount = a.CheckedFiles?.Count ?? 0;
            int bCount = b.CheckedFiles?.Count ?? 0;
            if (aCount != bCount) return false;

            if (a.Options.Count != b.Options.Count) return false;

            if (a.CheckedFiles != null && b.CheckedFiles != null)
            {
                if (!a.CheckedFiles.SetEquals(b.CheckedFiles)) return false;
            }

            foreach (var kvp in a.Options)
            {
                if (!b.Options.TryGetValue(kvp.Key, out bool bValue)) return false;
                if (kvp.Value != bValue) return false;
            }

            return true;
        }
    }
}