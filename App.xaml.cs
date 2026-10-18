using Auto237.Services;

namespace Auto237
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

#if WINDOWS
            // Simule un telephone sur PC : fenetre portrait centree.
            window.Width = 420;
            window.Height = 880;
            window.MinimumWidth = 360;
            window.MinimumHeight = 700;
            window.X = 100;
            window.Y = 40;
#endif

            // Le routage initial est declaratif : premier ShellContent = demarrage,
            // qui decide ensuite accueil vs connexion. Pas de navigation ici.

            return window;
        }
    }
}

