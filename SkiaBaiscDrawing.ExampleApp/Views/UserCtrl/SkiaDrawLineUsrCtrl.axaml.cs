using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace SkiaBasicDrawing.ExampleApp;

public partial class SkiaDrawLineUsrCtrl : UserControl
{

    private float[] _values = Array.Empty<float>();
    private int _version;

    public static readonly StyledProperty<float> MinValueProperty =
        AvaloniaProperty.Register<SkiaDrawLineUsrCtrl, float>(
            nameof(MinValue), float.MaxValue);

    public static readonly StyledProperty<float> MaxValueProperty =
        AvaloniaProperty.Register<SkiaDrawLineUsrCtrl, float>(
            nameof(MaxValue), float.MinValue);

    public static readonly StyledProperty<Color> LineColorProperty =
        AvaloniaProperty.Register<SkiaDrawLineUsrCtrl, Color>(
            nameof(LineColor), Colors.DeepSkyBlue);

    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<SkiaDrawLineUsrCtrl, double>(
            nameof(StrokeWidth), 1.0);

    public float MinValue
    {
        get => GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public float MaxValue
    {
        get => GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public Color LineColor
    {
        get => GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public SkiaDrawLineUsrCtrl()
    {
        InitializeComponent();
    }

    public void SetPoints(float[] values)
    {
        _values = values;
        _version++;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var step = (float)Bounds.Size.Width / _values.Length;
        var skPoints = new SKPoint[_values.Length];
        float x = 0;
        for (int i = 0; i < _values.Length; i++)
        {
            skPoints[i] = new SKPoint(x, (_values[i] - MinValue) / (MaxValue - MinValue) * (float)Bounds.Height);
            x += step;
        }
        var drawLineOp = new DrawLineOp(new Rect(Bounds.Size), skPoints, _version, LineColor.ToSKColor(), (float)StrokeWidth);
        context.Custom(drawLineOp);
    }

    private sealed class DrawLineOp : ICustomDrawOperation
    {
        private readonly SKPoint[] _pts;
        private readonly int _version;
        private readonly SKColor _color;
        private readonly float _strokeWidth;

        public Rect Bounds { get; }

        public DrawLineOp(Rect bounds, SKPoint[] pts, int version,
                          SKColor color, float strokeWidth)
        {
            Bounds = bounds;
            _pts = pts;
            _version = version;
            _color = color;
            _strokeWidth = strokeWidth;
        }

        public void Dispose() { }

        public bool HitTest(Point p) => Bounds.Contains(p);

        // 版本號與外觀都相同 → 回傳 true,場景圖重用上次繪製結果
        public bool Equals(ICustomDrawOperation? other) =>
            other is DrawLineOp o
            && o._version == _version
            && o.Bounds == Bounds
            && o._color == _color
            && o._strokeWidth == _strokeWidth;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null || _pts.Length < 2)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            canvas.Save();
            canvas.ClipRect(new SKRect(0, 0,
                (float)Bounds.Width, (float)Bounds.Height)); 

            using var paint = new SKPaint
            {
                Color = _color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = _strokeWidth,
                IsAntialias = _pts.Length < 50_000, 
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
            };
            
            canvas.DrawPoints(SKPointMode.Lines, _pts, paint);

            canvas.Restore();
        }
    }
}