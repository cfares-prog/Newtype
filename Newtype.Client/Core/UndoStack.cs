namespace Newtype.Client.Core
{
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();
    }

    public class UndoRedoService
    {
        private readonly Stack<IUndoableCommand> _undoStack = new();
        private readonly Stack<IUndoableCommand> _redoStack = new();

        public void ExecuteCommand(IUndoableCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); 
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }
    }
}
