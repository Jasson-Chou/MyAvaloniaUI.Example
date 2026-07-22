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

            const int count = 200_000;
            var pts = new float[count];
            var rnd = new Random();
            for (int i = 0; i < count; i++)
            {
                float y = 200f + 150f * MathF.Sin(i * 0.001f)
                               + (float)(rnd.NextDouble() * 8 - 4);
                pts[i] = y;
            }
            DrawLineControl.SetPoints(pts);
        }
    }
}