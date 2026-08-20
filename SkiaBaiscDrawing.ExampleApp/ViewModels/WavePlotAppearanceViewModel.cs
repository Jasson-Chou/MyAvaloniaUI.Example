using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.ViewModels
{
    public partial class WavePlotAppearanceViewModelBase:ViewModelBase
    {
        public WavePlotAppearanceViewModelBase()
        {
            _waveformLineColor = Colors.Red;
            _maxMinTextForeground = new SolidColorBrush(Colors.Black);
            _xAxisTitle = "Time (s)";
            _yAxisTitle = "Amplitude";
            _axisTitleForeground = new SolidColorBrush(Colors.Black);
            _gridLineColor = Colors.LightGray;
            _xAxisLineColor = Colors.Gray;
            _xAxisTextColor = Colors.Black;
            _cursorValueTextForeground = new SolidColorBrush(Colors.White);
            _cursorValueTextBackground = new SolidColorBrush(Colors.Black);
            _cursorLineBrush = new SolidColorBrush(Colors.Blue);
            _cursorPointerColor = new SolidColorBrush(Colors.Red);
            _waveformLineStrokeWidth = 2.0f;
            _background = new SolidColorBrush(Colors.White);
        }

        [ObservableProperty]
        private string? _name;

        [ObservableProperty]
        private Color _waveformLineColor;

        [ObservableProperty]
        private IBrush? _maxMinTextForeground;

        [ObservableProperty]
        private string? _xAxisTitle;

        [ObservableProperty]
        private string? _yAxisTitle;

        [ObservableProperty]
        private IBrush? _axisTitleForeground;

        [ObservableProperty]
        private Color _gridLineColor;

        [ObservableProperty]
        private Color _xAxisLineColor;

        [ObservableProperty]
        private Color _xAxisTextColor;

        [ObservableProperty]
        private IBrush? _cursorValueTextForeground;

        [ObservableProperty]
        private IBrush? _cursorValueTextBackground;

        [ObservableProperty]
        private IBrush? _cursorLineBrush;

        [ObservableProperty]
        private IBrush? _cursorPointerColor;

        [ObservableProperty]
        private float _waveformLineStrokeWidth;

        [ObservableProperty]
        private IBrush? _background;


    }

    public partial class WavePlotLightAppearanceViewModel : WavePlotAppearanceViewModelBase
    {
        public WavePlotLightAppearanceViewModel()
        {
            Name = "Light Theme";
            WaveformLineColor = Colors.Blue;
            MaxMinTextForeground = new SolidColorBrush(Colors.Black);
            XAxisTitle = "Time (sec)";
            YAxisTitle = "Amplitude (volt)";
            AxisTitleForeground =new SolidColorBrush(Colors.Black);
            GridLineColor = Colors.LightGray;
            XAxisLineColor = Colors.Gray;
            XAxisTextColor = Colors.Black;
            CursorValueTextForeground = new SolidColorBrush(Colors.White);
            CursorValueTextBackground = new SolidColorBrush(Colors.Black);
            CursorLineBrush = new SolidColorBrush(Colors.Blue);
            CursorPointerColor = new SolidColorBrush(Colors.Red);
            WaveformLineStrokeWidth = 2.0f;
            Background = new SolidColorBrush(Colors.White);
        }
    }

    public partial class WavePlotDarkAppearanceViewModel : WavePlotAppearanceViewModelBase
    {
        public WavePlotDarkAppearanceViewModel()
        {
            Name = "Dark Theme";
            WaveformLineColor = Colors.LimeGreen;
            MaxMinTextForeground = new SolidColorBrush(Colors.White);
            XAxisTitle = "Time (sec)";
            YAxisTitle = "Amplitude (volt)";
            AxisTitleForeground = new SolidColorBrush(Colors.White);
            GridLineColor = Colors.DarkGray;
            XAxisLineColor = Colors.Gray;
            XAxisTextColor = Colors.White;
            CursorValueTextForeground = new SolidColorBrush(Colors.Black);
            CursorValueTextBackground = new SolidColorBrush(Colors.White);
            CursorLineBrush = new SolidColorBrush(Colors.LimeGreen);
            CursorPointerColor = new SolidColorBrush(Colors.Red);
            WaveformLineStrokeWidth = 2.0f;
            Background = new SolidColorBrush(Colors.Black);
        }
    }
}
