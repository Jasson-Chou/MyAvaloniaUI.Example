using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaBasicDrawing.ExampleApp.Models;
using System;
using System.Collections.ObjectModel;

namespace SkiaBasicDrawing.ExampleApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            
            if (Design.IsDesignMode)
            {
                
            }
            else
            {
                var sineGen = new SineGenerator(UserSetting.Frequency, UserSetting.SampleRate, UserSetting.Amplitude);
                _waveformSimulator = new WaveformSimService(sineGen, _userSetting.PointCount);
            }
        }

        private readonly DispatcherTimer _randerTimer = new DispatcherTimer();
        private WaveformSimService _waveformSimulator;

        [ObservableProperty]
        private UserSettingViewModel _userSetting = new UserSettingViewModel();

        [RelayCommand]
        private void Run()
        {
            // Implement the logic to start the drawing process

            var sineGen = new SineGenerator(UserSetting.Frequency, UserSetting.SampleRate, UserSetting.Amplitude);
            _waveformSimulator = new WaveformSimService(sineGen, UserSetting.PointCount);
            _waveformSimulator.Start();
            _randerTimer.Tick += _randerTimer_Tick;
            _randerTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / UserSetting.Fps);
            _randerTimer.Start();
        }

        private void _randerTimer_Tick(object? sender, System.EventArgs e)
        {
            _waveformSimulator.Pull();
            if (_waveformSimulator.GetMinMax(out float min, out float max) &&
                true == UserSetting.AutoScale)
            {
                UserSetting.MinValue = min;
                UserSetting.MaxValue = max;
            }
            var values = _waveformSimulator.GetValues();
            
            Items.Clear();
            Items.AddRange(values);
        }

        [RelayCommand]
        private void Stop()
        {
            _randerTimer.Stop();
            _randerTimer.Tick -= _randerTimer_Tick;
            _waveformSimulator.Stop();
        }

        [ObservableProperty]
        private AvaloniaList<float> _items = new AvaloniaList<float>();
    }

    public partial class SettingViewModelBase: ViewModelBase
    {
        [ObservableProperty]
        private int _fps = 60;

        [ObservableProperty]
        private int _pointCount = 1000;

        [ObservableProperty]
        private float _sampleRate = 1000.0f;

        [ObservableProperty]
        private float _frequency = 5.0f;

        [ObservableProperty]
        private float _amplitude = 1.0f;

        [ObservableProperty]
        private float _minValue = -1.0f;

        [ObservableProperty]
        private float _maxValue = 1.0f;

        [ObservableProperty]
        private bool _autoScale = true;
    }

    public partial class UserSettingViewModel:SettingViewModelBase
    {

    }

    public static class SettingViewModelExtensions
    {
        public static void CopyFrom(this SettingViewModelBase target, SettingViewModelBase source)
        {
            target.Fps = source.Fps;
            target.PointCount = source.PointCount;
            target.SampleRate = source.SampleRate;
            target.Frequency = source.Frequency;
            target.Amplitude = source.Amplitude;
            target.MinValue = source.MinValue;
            target.MaxValue = source.MaxValue;
        }
    }

}
