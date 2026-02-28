using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public partial class RepeatViewModel : ContainerActionViewModel
{
    public override string Category => "Flow";
    public override string DisplayName => "Repeat";

    [ObservableProperty]
    private int _repeatCount = 1;

    [ObservableProperty]
    private string _counterVariable = "i";

    public RepeatViewModel()
    {
        Name = "Repeat";
    }

    public override IAction ToAction()
    {
        var action = new RepeatAction
        {
            Name = Name,
            RepeatCount = RepeatCount,
            CounterVariable = CounterVariable
        };
        action.Actions.AddRange(ChildrenToActions());
        return action;
    }
}
