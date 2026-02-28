using System.Collections.ObjectModel;
using UniversalTasker.UI.ViewModels;

namespace UniversalTasker.UI.Services;

public class AddActionCommand : IUndoableCommand
{
    private readonly ObservableCollection<ActionViewModel> _collection;
    private readonly ActionViewModel _item;
    private readonly int _index;

    public string Description => $"Add {_item.DisplayName}";

    public AddActionCommand(ObservableCollection<ActionViewModel> collection, ActionViewModel item, int index)
    {
        _collection = collection;
        _item = item;
        _index = index;
    }

    public void Execute() => _collection.Insert(_index, _item);
    public void Undo() => _collection.Remove(_item);
}

public class RemoveActionCommand : IUndoableCommand
{
    private readonly ObservableCollection<ActionViewModel> _collection;
    private readonly ActionViewModel _item;
    private readonly int _index;

    public string Description => $"Remove {_item.DisplayName}";

    public RemoveActionCommand(ObservableCollection<ActionViewModel> collection, ActionViewModel item, int index)
    {
        _collection = collection;
        _item = item;
        _index = index;
    }

    public void Execute() => _collection.RemoveAt(_index);
    public void Undo() => _collection.Insert(_index, _item);
}

public class MoveActionCommand : IUndoableCommand
{
    private readonly ObservableCollection<ActionViewModel> _collection;
    private readonly int _oldIndex;
    private readonly int _newIndex;

    public string Description => "Move action";

    public MoveActionCommand(ObservableCollection<ActionViewModel> collection, int oldIndex, int newIndex)
    {
        _collection = collection;
        _oldIndex = oldIndex;
        _newIndex = newIndex;
    }

    public void Execute() => _collection.Move(_oldIndex, _newIndex);
    public void Undo() => _collection.Move(_newIndex, _oldIndex);
}

public class AddTriggerCommand : IUndoableCommand
{
    private readonly ObservableCollection<TriggerViewModel> _collection;
    private readonly TriggerViewModel _item;
    private readonly int _index;

    public string Description => $"Add {_item.DisplayName} trigger";

    public AddTriggerCommand(ObservableCollection<TriggerViewModel> collection, TriggerViewModel item, int index)
    {
        _collection = collection;
        _item = item;
        _index = index;
    }

    public void Execute() => _collection.Insert(_index, _item);
    public void Undo() => _collection.Remove(_item);
}

public class RemoveTriggerCommand : IUndoableCommand
{
    private readonly ObservableCollection<TriggerViewModel> _collection;
    private readonly TriggerViewModel _item;
    private readonly int _index;

    public string Description => $"Remove {_item.DisplayName} trigger";

    public RemoveTriggerCommand(ObservableCollection<TriggerViewModel> collection, TriggerViewModel item, int index)
    {
        _collection = collection;
        _item = item;
        _index = index;
    }

    public void Execute() => _collection.RemoveAt(_index);
    public void Undo() => _collection.Insert(_index, _item);
}

public class AddVariableCommand : IUndoableCommand
{
    private readonly ObservableCollection<VariableItemViewModel> _collection;
    private readonly VariableItemViewModel _item;
    private readonly int _index;

    public string Description => $"Add variable '{_item.Key}'";

    public AddVariableCommand(ObservableCollection<VariableItemViewModel> collection, VariableItemViewModel item, int index)
    {
        _collection = collection;
        _item = item;
        _index = index;
    }

    public void Execute() => _collection.Insert(_index, _item);
    public void Undo() => _collection.Remove(_item);
}

public class RemoveVariableCommand : IUndoableCommand
{
    private readonly ObservableCollection<VariableItemViewModel> _collection;
    private readonly VariableItemViewModel _item;
    private readonly int _index;

    public string Description => $"Remove variable '{_item.Key}'";

    public RemoveVariableCommand(ObservableCollection<VariableItemViewModel> collection, VariableItemViewModel item, int index)
    {
        _collection = collection;
        _item = item;
        _index = index;
    }

    public void Execute() => _collection.RemoveAt(_index);
    public void Undo() => _collection.Insert(_index, _item);
}
