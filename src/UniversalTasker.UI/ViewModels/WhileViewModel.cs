using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;

namespace UniversalTasker.UI.ViewModels;

public partial class WhileViewModel : ContainerActionViewModel
{
    public override string Category => "Flow";
    public override string DisplayName => "While Loop";

    [ObservableProperty]
    private string _leftOperand = "";

    [ObservableProperty]
    private ComparisonOperator _operator = ComparisonOperator.Equals;

    [ObservableProperty]
    private string _rightOperand = "";

    [ObservableProperty]
    private int _maxIterations = 10000;

    public WhileViewModel()
    {
        Name = "While Loop";
    }

    public override IAction ToAction()
    {
        var action = new WhileAction
        {
            Name = Name,
            Condition = new Condition
            {
                LeftOperand = LeftOperand,
                Operator = Operator,
                RightOperand = RightOperand
            },
            MaxIterations = MaxIterations
        };
        action.Actions.AddRange(ChildrenToActions());
        return action;
    }
}
