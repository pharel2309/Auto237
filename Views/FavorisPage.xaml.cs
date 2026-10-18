using Auto237.ViewModels;

namespace Auto237.Views;

public partial class FavorisPage : ContentPage
{
    public FavorisPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (BindingContext is FavorisViewModel vm) await vm.ChargerAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Favoris", ex.Message, "OK");
        }
    }
}
