using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CounterApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            _count = 0;
        }

        [ObservableProperty]
        private int _count;

        [RelayCommand]
        private void IncrementCount()
        {
            Count++;
        }
    }
}
