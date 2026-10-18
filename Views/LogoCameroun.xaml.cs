namespace Auto237.Views;

public partial class LogoCameroun : ContentView
{
    public static readonly BindableProperty TailleProperty =
        BindableProperty.Create(nameof(Taille), typeof(double), typeof(LogoCameroun), 22.0,
            propertyChanged: (b, o, n) =>
            {
                if (b is LogoCameroun c && n is double d)
                    c.LogoLabel.FontSize = d;
            });

    public double Taille
    {
        get => (double)GetValue(TailleProperty);
        set => SetValue(TailleProperty, value);
    }

    public LogoCameroun()
    {
        InitializeComponent();
        LogoLabel.FontSize = Taille;
    }
}
