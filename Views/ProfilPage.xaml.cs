using Auto237.ViewModels;

namespace Auto237.Views;

public partial class ProfilPage : ContentPage
{
    public ProfilPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProfilViewModel vm) await vm.ChargerAsync();
    }
}
