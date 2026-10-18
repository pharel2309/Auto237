namespace Auto237;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // connexion/inscription/recuperation/profil deja declares en ShellContent XAML.
        // On garde uniquement la route push detail (appelee depuis plusieurs parents).
        Routing.RegisterRoute("detail", typeof(Views.DetailPage));
    }
}

