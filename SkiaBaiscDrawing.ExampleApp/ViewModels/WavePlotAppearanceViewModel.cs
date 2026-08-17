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
            _maxMinTextForeground = Brushes.Black;
            _xAxisTitle = "Time (s)";
            _yAxisTitle = "Amplitude";
            _axisTitleForeground = Brushes.Black;
            _gridLineColor = Colors.LightGray;
            _timeAxisLineColor = Colors.Gray;
            _timeAxisTextColor = Colors.Black;
            _cursorValueTextForeground = Brushes.White;
            _cursorValueTextBackground = Brushes.Black;
            _cursorLineBrush = Brushes.Blue;
            _cursorPointerColor = Brushes.Red;
            _waveformLineStrokeWidth = 2.0f;
            _background = Brushes.White;
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
        private Color _timeAxisLineColor;

        [ObservableProperty]
        private Color _timeAxisTextColor;

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
            MaxMinTextForeground = Brushes.Black;
            XAxisTitle = "Time (s)";
            YAxisTitle = "Amplitude";
            AxisTitleForeground = Brushes.Black;
            GridLineColor = Colors.LightGray;
            TimeAxisLineColor = Colors.Gray;
            TimeAxisTextColor = Colors.Black;
            CursorValueTextForeground = Brushes.White;
            CursorValueTextBackground = Brushes.Black;
            CursorLineBrush = Brushes.Blue;
            CursorPointerColor = Brushes.Red;
            WaveformLineStrokeWidth = 2.0f;
            Background = Brushes.White;
        }
    }

    public partial class WavePlotDarkAppearanceViewModel : WavePlotAppearanceViewModelBase
    {
        public WavePlotDarkAppearanceViewModel()
        {
            Name = "Dark Theme";
            WaveformLineColor = Colors.LimeGreen;
            MaxMinTextForeground = Brushes.White;
            XAxisTitle = "Time (s)";
            YAxisTitle = "Amplitude";
            AxisTitleForeground = Brushes.White;
            GridLineColor = Colors.DarkGray;
            TimeAxisLineColor = Colors.Gray;
            TimeAxisTextColor = Colors.White;
            CursorValueTextForeground = Brushes.Black;
            CursorValueTextBackground = Brushes.White;
            CursorLineBrush = Brushes.LimeGreen;
            CursorPointerColor = Brushes.Red;
            WaveformLineStrokeWidth = 2.0f;
            Background = Brushes.Black;
        }
    }
}
