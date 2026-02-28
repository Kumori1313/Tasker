using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Input;
using UniversalTasker.UI.Services;

namespace UniversalTasker.UI.ViewModels;

public partial class MouseClickViewModel : ActionViewModel
{
    public override string Category => "Input";
    public override string DisplayName => "Mouse Click";

    [ObservableProperty]
    private MouseButton _button = MouseButton.Left;

    [ObservableProperty]
    private int _x;

    [ObservableProperty]
    private int _y;

    [ObservableProperty]
    private int _clickCount = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PickLocationCommand))]
    private bool _isPicking;

    public MouseClickViewModel()
    {
        Name = "Mouse Click";
    }

    [RelayCommand(CanExecute = nameof(CanPickLocation))]
    private async Task PickLocation()
    {
        IsPicking = true;
        try
        {
            using var picker = new LocationPickerService();
            var result = await picker.PickLocationAsync();
            if (result.HasValue)
            {
                X = result.Value.X;
                Y = result.Value.Y;
            }
        }
        finally
        {
            IsPicking = false;
        }
    }

    private bool CanPickLocation() => !IsPicking;

    public override IAction ToAction()
    {
        return new MouseClickAction
        {
            Name = Name,
            Button = Button,
            X = X,
            Y = Y,
            ClickCount = ClickCount
        };
    }
}
