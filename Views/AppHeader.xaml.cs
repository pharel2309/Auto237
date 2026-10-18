namespace Auto237.Views;

public partial class AppHeader : ContentView
{
    public AppHeader() => InitializeComponent();

    private async void OnProfilClicked(object? sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync("//profil"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }
}
