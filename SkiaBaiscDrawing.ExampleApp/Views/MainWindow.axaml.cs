using Avalonia.Controls;
using SkiaSharp;
using System;

namespace SkiaBasicDrawing.ExampleApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            const int count = 1000;
            var pts = new float[count];
            var rnd = new Random();
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float y = 200f + 150f * MathF.Sin(i * 0.001f)
                               + (float)(rnd.NextDouble() * 8 - 4);
                pts[i] = y;
                if (y < min) min = y;
                if (y > max) max = y;
            }
            DrawLineControl.MinValue = min;
            DrawLineControl.MaxValue = max;
            DrawLineControl.SetValues(pts);
        }
    }
}