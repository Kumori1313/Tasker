using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public partial class SetVariableViewModel : ActionViewModel
{
    public override string Category => "Variables";
    public override string DisplayName => "Set Variable";

    [ObservableProperty]
    private string _variableName = "";

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private bool _evaluateAsExpression;

    public SetVariableViewModel()
    {
        Name = "Set Variable";
    }

    public override IAction ToAction()
    {
        return new SetVariableAction
        {
            Name = Name,
            VariableName = VariableName,
            Value = Value,
            EvaluateAsExpression = EvaluateAsExpression
        };
    }
}
