using Auto237.ViewModels;

namespace Auto237.Views;

public partial class DemarragePage : ContentPage
{
    private bool _dejaLance;

    public DemarragePage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_dejaLance) return;
        _dejaLance = true;
        try
        {
            if (BindingContext is DemarrageViewModel vm)
            {
                // Timeout 8s : si la decision bloque, on affiche un message au lieu de rester fige.
                var tache = vm.DeciderAsync();
                var delai = Task.Delay(8000);
                var finie = await Task.WhenAny(tache, delai);
                if (finie != tache && string.IsNullOrEmpty(vm.Erreur))
                    vm.Erreur = "Démarrage lent. Vérifie la connexion puis relance.";
                await tache;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DemarragePage: {ex.Message}");
        }
    }

    protected override bool OnBackButtonPressed() => true;
}
