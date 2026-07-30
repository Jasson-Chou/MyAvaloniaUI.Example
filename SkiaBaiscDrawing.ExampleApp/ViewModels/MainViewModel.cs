using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SkiaBasicDrawing.ExampleApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int _fps = 60;

        [ObservableProperty]
        private int _pointCount = 1000;

        [ObservableProperty]
        private float _minValue = -1.0f;

        [ObservableProperty]
        private float _maxValue = 1.0f;

        [ObservableProperty]
        private AvaloniaList<float> _values = new AvaloniaList<float>();
    }
}
