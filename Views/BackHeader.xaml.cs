namespace Auto237.Views;

public partial class BackHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(BackHeader), string.Empty);

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(System.Windows.Input.ICommand), typeof(BackHeader));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public System.Windows.Input.ICommand? BackCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public BackHeader() => InitializeComponent();

    private void OnBackClicked(object? sender, EventArgs e)
    {
        try
        {
            if (BackCommand == null)
            {
                System.Diagnostics.Debug.WriteLine("BackHeader : BackCommand null, vérifie le nom de la commande dans le ViewModel.");
                return;
            }
            BackCommand.Execute(null);
        }
        catch { }
    }
}
