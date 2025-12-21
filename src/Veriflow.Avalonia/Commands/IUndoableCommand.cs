namespace Veriflow.Avalonia.Commands
{
    public interface IUndoableCommand
    {
        string Description { get; }
        void Execute();
        void UnExecute(); // Used internally or via Undo
        void Undo();      // Alias if CommandHistory expects Undo
    }
}
