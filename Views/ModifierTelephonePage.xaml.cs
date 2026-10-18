namespace Auto237.Views;

public partial class ModifierTelephonePage : ContentPage
{
    public ModifierTelephonePage()
    {
        InitializeComponent();
        _ = ((ViewModels.ModifierTelephoneViewModel)BindingContext).ChargerAsync();
    }
}