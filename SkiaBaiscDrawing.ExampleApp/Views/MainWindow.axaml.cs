using Avalonia.Controls;
using Avalonia.Threading;
using HarfBuzzSharp;
using SkiaBasicDrawing.ExampleApp.Models;
using SkiaSharp;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //const int count = 1000;
            const int count = 50_000;
            var pts = new float[count];
            var rnd = new Random();
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float y = 150f * MathF.Sin(i * 0.1f)
                               + (float)(rnd.NextDouble() * 8 - 4)
                               ;
                pts[i] = y;
                if (y < min) min = y;
                if (y > max) max = y;
            }
            DrawLineControl.MinValue = min;
            DrawLineControl.MaxValue = max;
            DrawLineControl.SetValues(pts);
        }

        private SineGenerator _sineGenerator = new SineGenerator(5, 1000, 1.0);
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        private Stopwatch _stopwatch = new Stopwatch();
        private TimeSpan _lastTimeSpan = TimeSpan.Zero;
        private DropOldestQueue<float> buffer;
        private void runButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var fps = fpsNumericUpDown.Value;
            if(fps is null) throw new InvalidOperationException("FPS value is null.");

            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / (double)fps); // Set FPS based on NumericUpDown value
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
            buffer = new DropOldestQueue<float>(DrawLineControl.PointCount);

            _sineGenerator.Reset();
            _stopwatch.Start();
            _dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            var spendTime = _stopwatch.Elapsed;
            var deltaTime = spendTime - _lastTimeSpan;
            _lastTimeSpan = spendTime;

            UpdateValues(spendTime, deltaTime);
        }

        private void UpdateValues(TimeSpan spendTime, TimeSpan deltaTime)
        {
            var buff = _sineGenerator.GenerateF(spendTime, deltaTime);
            buffer.EnqueueRange(buff);
            DrawLineControl.SetValues(buffer.ToArray());
        }

        private void stopButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _stopwatch?.Stop();
            _dispatcherTimer?.Stop();

            if(_stopwatch is not null)
            {
                var spendTime = _stopwatch.Elapsed;
                var deltaTime = spendTime - _lastTimeSpan;
                _lastTimeSpan = spendTime;
                UpdateValues(spendTime, deltaTime);
            }

            
        }
    }
}