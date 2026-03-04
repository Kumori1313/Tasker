using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public abstract partial class ActionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    public abstract string Category { get; }
    public abstract string DisplayName { get; }

    public abstract IAction ToAction();

    public static ActionViewModel FromAction(IAction action)
    {
        return action switch
        {
            MouseClickAction mca => new MouseClickViewModel
            {
                Name = mca.Name,
                Button = mca.Button,
                X = mca.X,
                Y = mca.Y,
                ClickCount = mca.ClickCount
            },
            KeyPressAction kpa => new KeyPressViewModel
            {
                Name = kpa.Name,
                VirtualKeyCode = kpa.VirtualKeyCode,
                Ctrl = kpa.Ctrl,
                Alt = kpa.Alt,
                Shift = kpa.Shift
            },
            DelayAction da => new DelayViewModel
            {
                Name = da.Name,
                DurationMs = da.DurationMs
            },
            SetVariableAction sva => new SetVariableViewModel
            {
                Name = sva.Name,
                VariableName = sva.VariableName,
                Value = sva.Value,
                EvaluateAsExpression = sva.EvaluateAsExpression
            },
            RepeatAction ra => CreateRepeatViewModel(ra),
            WhileAction wa => CreateWhileViewModel(wa),
            ConditionAction ca => CreateConditionViewModel(ca),
            BreakAction ba => new BreakViewModel { Name = ba.Name },
            ContinueAction cont => new ContinueViewModel { Name = cont.Name },
            _ => new GenericPluginActionViewModel(action)
        };
    }

    private static RepeatViewModel CreateRepeatViewModel(RepeatAction ra)
    {
        var vm = new RepeatViewModel
        {
            Name = ra.Name,
            RepeatCount = ra.RepeatCount,
            CounterVariable = ra.CounterVariable
        };
        vm.PopulateChildren(ra.Actions);
        return vm;
    }

    private static WhileViewModel CreateWhileViewModel(WhileAction wa)
    {
        var vm = new WhileViewModel
        {
            Name = wa.Name,
            LeftOperand = wa.Condition.LeftOperand,
            Operator = wa.Condition.Operator,
            RightOperand = wa.Condition.RightOperand,
            MaxIterations = wa.MaxIterations
        };
        vm.PopulateChildren(wa.Actions);
        return vm;
    }

    private static ConditionViewModel CreateConditionViewModel(ConditionAction ca)
    {
        var vm = new ConditionViewModel
        {
            Name = ca.Name,
            LeftOperand = ca.Condition.LeftOperand,
            Operator = ca.Condition.Operator,
            RightOperand = ca.Condition.RightOperand
        };
        vm.PopulateBranches(ca.ThenActions, ca.ElseActions);
        return vm;
    }
}
