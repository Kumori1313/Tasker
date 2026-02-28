using CommunityToolkit.Mvvm.ComponentModel;

namespace UniversalTasker.UI.ViewModels;

public partial class VariableItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = "";

    [ObservableProperty]
    private string _value = "";
}
