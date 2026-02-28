using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public partial class ContinueViewModel : ActionViewModel
{
    public override string Category => "Flow";
    public override string DisplayName => "Continue";

    public ContinueViewModel()
    {
        Name = "Continue";
    }

    public override IAction ToAction()
    {
        return new ContinueAction { Name = Name };
    }
}
