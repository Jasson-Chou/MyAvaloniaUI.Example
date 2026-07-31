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

            ////const int count = 1000;
            //const int count = 50_000;
            //float[] pts = new float[count];
            //DropOldestQueue<float> oldestQueue = new DropOldestQueue<float>(count);
            //SineGenerator sine = new SineGenerator(5, sampleRate: 1000, amplitude: 3);
            //sine.Fill(pts);
            //oldestQueue.EnqueueRange(pts);
            //oldestQueue.GetMinMax(out float min, out float max);
            //DrawLineControl.MinValue = min;
            //DrawLineControl.MaxValue = max;
            //DrawLineControl.Items = pts;
        }

        //private SineGenerator _sineGenerator = new SineGenerator(5, 1000, 1.0);
        //private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        //private Stopwatch _stopwatch = new Stopwatch();
        //private TimeSpan _lastTimeSpan = TimeSpan.Zero;
        //private DropOldestQueue<float> buffer;
        //private void runButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        //{
        //    var fps = fpsNumericUpDown.Value;
        //    if(fps is null) throw new InvalidOperationException("FPS value is null.");

        //    _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / (double)fps); // Set FPS based on NumericUpDown value
        //    _dispatcherTimer.Tick += DispatcherTimer_Tick;
        //    buffer = new DropOldestQueue<float>(DrawLineControl.PointCount);

        //    _sineGenerator.Reset();
        //    _lastTimeSpan = TimeSpan.Zero;
        //    _stopwatch.Restart();
        //    _dispatcherTimer.Start();
        //}

        //private void DispatcherTimer_Tick(object? sender, EventArgs e)
        //{
        //    UpdateValues();
        //}

        //private void UpdateValues()
        //{
        //    var currentTime = _stopwatch.Elapsed;
        //    var deltaTime = currentTime - _lastTimeSpan;
        //    var buff = _sineGenerator.GenerateF(_lastTimeSpan, deltaTime);
        //    _lastTimeSpan = currentTime;
        //    buffer.EnqueueRange(buff);
        //    buffer.GetMinMax(out float min, out float max);
        //    DrawLineControl.MinValue = min;
        //    DrawLineControl.MaxValue = max;
        //    DrawLineControl.Items = null; // Clear the items before updating to avoid potential issues
        //    DrawLineControl.Items = buffer.ToArray();
        //}

        //private void stopButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        //{
            

        //    if(_dispatcherTimer is not null && _dispatcherTimer.IsEnabled)
        //    {
        //        _dispatcherTimer.Stop();
        //        _dispatcherTimer.Tick -= DispatcherTimer_Tick;
        //    }
            
        //    if (_stopwatch is not null)
        //    {
        //        _stopwatch.Stop();
        //        UpdateValues();
        //    }
        //}
    }
}