namespace Auto237.Views;

public partial class ModifierAgencePage : ContentPage
{
    public ModifierAgencePage()
    {
        InitializeComponent();
        _ = ((ViewModels.ModifierAgenceViewModel)BindingContext).ChargerAsync();
    }
}