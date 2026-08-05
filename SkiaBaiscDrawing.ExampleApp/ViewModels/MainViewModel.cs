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
        public MainViewModel(IUiTimer uiTimer, IWaveformRunService waveformRunService, IWaveformGenFactory waveformGenFactory)
        {
            _uiTimer = uiTimer;
            _waveformRunService = waveformRunService;
            _waveformGenFactory = waveformGenFactory;
            UserSetting = new UserSettingViewModel();
            if (Design.IsDesignMode)
            {
                
            }
            else
            {
                UserSetting.PropertyChanged += UserSetting_PropertyChanged;
            }
        }

        private void UserSetting_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(UserSetting.AutoScale):
                    if (UserSetting.AutoScale)
                    {
                        _waveformRunService.GetMinMax(out float min, out float max);
                        UserSetting.MinValue = min;
                        UserSetting.MaxValue = max;
                    }
                    break;
            }
        }
        
        private readonly IUiTimer _uiTimer;
        private readonly IWaveformRunService _waveformRunService;
        private readonly IWaveformGenFactory _waveformGenFactory;

        [ObservableProperty]
        private UserSettingViewModel _userSetting;

        [RelayCommand(CanExecute = nameof(CanRun))]
        private void Run()
        {
            // Implement the logic to start the drawing process
            Items.Clear();
            IsRunning = true;
            PointCount = UserSetting.PointCount;
            var waveformGen = _waveformGenFactory.Create(WaveformType.Sine, UserSetting.Frequency, UserSetting.SampleRate, UserSetting.Amplitude);
            _waveformRunService.ResetSimulator(waveformGen);

            if (_waveformRunService.BufferSize != UserSetting.PointCount)
            {
                _waveformRunService.SetBufferSize(UserSetting.PointCount);
            }

            _waveformRunService.Start();

            _uiTimer.Tick += _randerTimer_Tick;
            _uiTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / UserSetting.Fps);
            _uiTimer.Start();
        }

        private void _randerTimer_Tick(object? sender, System.EventArgs e)
        {
            _waveformRunService.Pull();
            if (_waveformRunService.GetMinMax(out float min, out float max) &&
                true == UserSetting.AutoScale)
            {
                UserSetting.MinValue = min;
                UserSetting.MaxValue = max;
            }
            var values = _waveformRunService.GetValues();

            //// for avalonialist, we can use ReplaceAll to update the collection efficiently
            Items.Clear();
            Items.AddRange(values);

            //Items.ReplaceAll(values);

        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            _uiTimer.Stop();
            _uiTimer.Tick -= _randerTimer_Tick;
            _waveformRunService.Stop();
            IsRunning = false;
        }

        [RelayCommand(CanExecute = nameof(CanRun))]
        private void GenerateWaveform()
        {
            PointCount = UserSetting.PointCount;
            var waveformGen = _waveformGenFactory.Create(WaveformType.Sine, UserSetting.Frequency, UserSetting.SampleRate, UserSetting.Amplitude);
            var buffer = waveformGen.GenerateF(PointCount);
            Items.Clear();
            Items.AddRange(buffer);
        }

        [RelayCommand(CanExecute = nameof(CanRun))]
        private void ClearItems()
        {
            Items.Clear();
        }

        private bool CanRun()
        {
            return !IsRunning;
        }

        private bool CanStop()
        {
            return IsRunning;
        }


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        [NotifyCanExecuteChangedFor(nameof(GenerateWaveformCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearItemsCommand))]
        private bool _isRunning = false;

        [ObservableProperty]
        private int _pointCount = 1000;

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
