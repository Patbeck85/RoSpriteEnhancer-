using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class UndoRedoManagerTests
{
    private class TestState
    {
        public int Value { get; set; }
        public string Name { get; set; } = "";

        public override bool Equals(object? obj)
        {
            if (obj is TestState other)
                return Value == other.Value && Name == other.Name;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(Value, Name);
    }

    [Fact]
    public void Constructor_DefaultMaxHistory_IsAtLeast10()
    {
        var manager = new UndoRedoManager<TestState>();
        Assert.True(manager.MaxHistory >= 10);
    }

    [Fact]
    public void Constructor_MaxHistoryLessThan10_IsRaisedTo10()
    {
        var manager = new UndoRedoManager<TestState>(1);
        Assert.True(manager.MaxHistory >= 10);
    }

    [Fact]
    public void InitialState_CanUndoFalse_CanRedoFalse()
    {
        var manager = new UndoRedoManager<TestState>();
        var state = new TestState { Value = 1 };
        manager.SetInitial(state);

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
        Assert.Equal(1, manager.Current?.Value);
    }

    [Fact]
    public void Push_NewState_CanUndoTrue()
    {
        var manager = new UndoRedoManager<TestState>();
        manager.SetInitial(new TestState { Value = 1 });

        manager.Push(new TestState { Value = 2 });

        Assert.True(manager.CanUndo);
        Assert.Equal(2, manager.Current?.Value);
    }

    [Fact]
    public void Undo_ReturnsPreviousState()
    {
        var manager = new UndoRedoManager<TestState>();
        manager.SetInitial(new TestState { Value = 1 });
        manager.Push(new TestState { Value = 2 });

        var previous = manager.Undo();

        Assert.Equal(1, previous?.Value);
        Assert.Equal(1, manager.Current?.Value);
        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void Redo_ReturnsUndoneState()
    {
        var manager = new UndoRedoManager<TestState>();
        manager.SetInitial(new TestState { Value = 1 });
        manager.Push(new TestState { Value = 2 });
        manager.Undo();

        var redone = manager.Redo();

        Assert.Equal(2, redone?.Value);
        Assert.Equal(2, manager.Current?.Value);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Undo_WhenNothingToUndo_ReturnsNull()
    {
        var manager = new UndoRedoManager<TestState>();
        manager.SetInitial(new TestState { Value = 1 });

        var result = manager.Undo();

        Assert.Null(result);
    }

    [Fact]
    public void Redo_WhenNothingToRedo_ReturnsNull()
    {
        var manager = new UndoRedoManager<TestState>();
        manager.SetInitial(new TestState { Value = 1 });

        var result = manager.Redo();

        Assert.Null(result);
    }

    [Fact]
    public void Clear_RemovesAllHistory()
    {
        var manager = new UndoRedoManager<TestState>();
        manager.SetInitial(new TestState { Value = 1 });
        manager.Push(new TestState { Value = 2 });
        manager.Push(new TestState { Value = 3 });

        manager.Clear();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
        Assert.Null(manager.Current);
    }

    [Fact]
    public void Push_NullState_ThrowsArgumentNullException()
    {
        var manager = new UndoRedoManager<TestState>();
        Assert.Throws<ArgumentNullException>(() => manager.Push(null!));
    }

    [Fact]
    public void SetInitial_NullState_ThrowsArgumentNullException()
    {
        var manager = new UndoRedoManager<TestState>();
        Assert.Throws<ArgumentNullException>(() => manager.SetInitial(null!));
    }

    [Fact]
    public void Clone_CreatesCopy()
    {
        var original = new TestState { Value = 42, Name = "Test" };
        var clone = UndoRedoManager<TestState>.Clone(original);

        Assert.NotSame(original, clone);
        Assert.Equal(42, clone.Value);
        Assert.Equal("Test", clone.Name);
    }

    [Fact]
    public void Clone_NullState_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UndoRedoManager<TestState>.Clone(null!));
    }

    [Fact]
    public void Push_MultipleStates_LimitedByMaxHistory()
    {
        var manager = new UndoRedoManager<TestState>(3);
        manager.SetInitial(new TestState { Value = 0 });
        manager.Push(new TestState { Value = 1 });
        manager.Push(new TestState { Value = 2 });
        manager.Push(new TestState { Value = 3 });
        manager.Push(new TestState { Value = 4 });

        // MaxHistory begrenzt den Undo-Stack
        Assert.True(manager.UndoCount <= manager.MaxHistory);
        Assert.Equal(4, manager.Current?.Value);
    }

    [Fact]
    public void Push_SameState_DoesNotCreateDuplicate()
    {
        var manager = new UndoRedoManager<TestState>();
        manager.SetInitial(new TestState { Value = 1 });

        manager.Push(new TestState { Value = 1 });

        Assert.False(manager.CanUndo);
        Assert.Equal(1, manager.Current?.Value);
    }
}
