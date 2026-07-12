using CommunityToolkit.Mvvm.ComponentModel;

namespace MyFirstAvaloniaApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";

        [ObservableProperty]
        private ExampleViewModel _exampleViewModel = new ExampleViewModel();
    }


    public partial class ExampleViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _exampleText = "This is an example ViewModel.";
    }
}
