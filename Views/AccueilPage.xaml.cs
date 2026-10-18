using Auto237.ViewModels;

namespace Auto237.Views;

public partial class AccueilPage : ContentPage
{
    public AccueilPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AccueilViewModel vm)
            await vm.ChargerAsync();
    }

    private async void OnFiltreChanged(object? sender, EventArgs e)
    {
        if (BindingContext is AccueilViewModel vm)
            await vm.ChargerAsync();
    }

    private async void OnProfilClicked(object? sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync("//profil"); }
        catch (Exception ex) { await DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }
}
