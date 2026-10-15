using LocationTracker.ViewModels;

namespace LocationTracker.Views;

/// <summary>
/// Main page of the app. All logic lives in <see cref="MainViewModel"/>;
/// the code-behind only wires up the binding context and the page lifecycle.
/// </summary>
public partial class MainView : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load stored points from SQLite and start tracking the first time the page shows.
        if (_viewModel.LoadCommand.CanExecute(null))
        {
            _viewModel.LoadCommand.Execute(null);
        }
    }
}
