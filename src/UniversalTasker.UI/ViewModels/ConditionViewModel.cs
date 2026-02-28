using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;

namespace UniversalTasker.UI.ViewModels;

public partial class ConditionViewModel : ActionViewModel
{
    public override string Category => "Flow";
    public override string DisplayName => "If/Else";

    [ObservableProperty]
    private string _leftOperand = "";

    [ObservableProperty]
    private ComparisonOperator _operator = ComparisonOperator.Equals;

    [ObservableProperty]
    private string _rightOperand = "";

    public ObservableCollection<ActionViewModel> ThenActions { get; } = new();
    public ObservableCollection<ActionViewModel> ElseActions { get; } = new();

    [ObservableProperty]
    private int _nestingLevel;

    public ConditionViewModel()
    {
        Name = "If/Else";
    }

    public override IAction ToAction()
    {
        var action = new ConditionAction
        {
            Name = Name,
            Condition = new Condition
            {
                LeftOperand = LeftOperand,
                Operator = Operator,
                RightOperand = RightOperand
            }
        };
        action.ThenActions.AddRange(ThenActions.Select(vm => vm.ToAction()));
        action.ElseActions.AddRange(ElseActions.Select(vm => vm.ToAction()));
        return action;
    }

    public void PopulateBranches(IEnumerable<IAction> thenActions, IEnumerable<IAction> elseActions)
    {
        ThenActions.Clear();
        foreach (var action in thenActions)
        {
            ThenActions.Add(ActionViewModel.FromAction(action));
        }

        ElseActions.Clear();
        foreach (var action in elseActions)
        {
            ElseActions.Add(ActionViewModel.FromAction(action));
        }
    }
}
