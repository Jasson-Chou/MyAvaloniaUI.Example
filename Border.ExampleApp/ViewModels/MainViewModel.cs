using CommunityToolkit.Mvvm.ComponentModel;

namespace Border.ExampleApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Hi! Jasson.";
    }
}
