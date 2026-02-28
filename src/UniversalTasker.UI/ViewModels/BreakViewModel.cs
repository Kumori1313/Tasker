using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public partial class BreakViewModel : ActionViewModel
{
    public override string Category => "Flow";
    public override string DisplayName => "Break";

    public BreakViewModel()
    {
        Name = "Break";
    }

    public override IAction ToAction()
    {
        return new BreakAction { Name = Name };
    }
}
