namespace Auto237.Views;

public partial class DetailPage : ContentPage
{
    private bool _autoDefile;
    private CancellationTokenSource? _cts;

    public DetailPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _autoDefile = true;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                while (_autoDefile && !token.IsCancellationRequested)
                {
                    await Task.Delay(3000, token);
                    if (!_autoDefile || token.IsCancellationRequested) break;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (BindingContext is ViewModels.DetailViewModel vm && vm.PhotosUrls.Count > 1)
                            vm.Position = (vm.Position + 1) % vm.PhotosUrls.Count;
                    });
                }
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    protected override void OnDisappearing()
    {
        _autoDefile = false;
        try { _cts?.Cancel(); } catch { }
        base.OnDisappearing();
    }
}
