using Auto237.ViewModels;

namespace Auto237.Views;

public partial class ChoisirTypePage : ContentPage
{
    public ChoisirTypePage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ChoisirTypeViewModel vm)
            await vm.ChargerAsync();
    }
}
