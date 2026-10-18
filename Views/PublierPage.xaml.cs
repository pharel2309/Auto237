using Auto237.Services;
using Auto237.ViewModels;

namespace Auto237.Views;

public partial class PublierPage : ContentPage
{
    public PublierPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var auth = new AuthService();
            if (!await auth.EstConnecteAsync())
            {
                await Shell.Current.GoToAsync("//connexion");
            }
            else if (BindingContext is PublierViewModel vm)
            {
                await vm.RafraichirQuotaAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Navigation", ex.Message, "OK");
        }
    }
}

