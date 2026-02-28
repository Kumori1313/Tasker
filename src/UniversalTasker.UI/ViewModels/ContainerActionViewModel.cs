using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public abstract partial class ContainerActionViewModel : ActionViewModel
{
    public ObservableCollection<ActionViewModel> Children { get; } = new();

    [ObservableProperty]
    private int _nestingLevel;

    protected List<IAction> ChildrenToActions()
    {
        return Children.Select(vm => vm.ToAction()).ToList();
    }

    public void PopulateChildren(IEnumerable<IAction> actions)
    {
        Children.Clear();
        foreach (var action in actions)
        {
            var vm = ActionViewModel.FromAction(action);
            if (vm is ContainerActionViewModel container)
            {
                container.NestingLevel = NestingLevel + 1;
            }
            Children.Add(vm);
        }
    }
}
