using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public partial class DelayViewModel : ActionViewModel
{
    public override string Category => "Flow";
    public override string DisplayName => "Delay";

    [ObservableProperty]
    private int _durationMs = 1000;

    public DelayViewModel()
    {
        Name = "Delay";
    }

    public override IAction ToAction()
    {
        return new DelayAction
        {
            Name = Name,
            DurationMs = DurationMs
        };
    }
}
