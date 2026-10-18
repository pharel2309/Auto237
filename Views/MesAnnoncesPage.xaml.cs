using Auto237.ViewModels;

namespace Auto237.Views;

public partial class MesAnnoncesPage : ContentPage
{
    public MesAnnoncesPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MesAnnoncesViewModel vm) await vm.ChargerAsync();
    }
}
