using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Views.UserCtrl
{
    public class SkiaDrawWaveformScopeUsrCtrl : UserControl
    {

        private float[] _values = Array.Empty<float>();
        private int _version;

        public static readonly StyledProperty<float> MinValueProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MinValue), float.MaxValue);

        public static readonly StyledProperty<float> MaxValueProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MaxValue), float.MinValue);

        public static readonly StyledProperty<Color> LineColorProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, Color>(
                nameof(LineColor), Colors.DeepSkyBlue);

        public static readonly StyledProperty<float> StrokeWidthProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(StrokeWidth), 1.0f);

        public static readonly StyledProperty<float> XScaleProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(XScale), 1.0f);

        public static readonly StyledProperty<float> YScaleProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(YScale), 1.0f);

        public static readonly StyledProperty<float> XOffsetProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(XOffset), 0.0f, coerce: (o, value) =>
                {
                    var ctrl = (SkiaDrawWaveformScopeUsrCtrl)o;
                    return Math.Clamp(value, 0.0f, ctrl.MaxXOffset);
                });

        public static readonly DirectProperty<SkiaDrawWaveformScopeUsrCtrl, float> MaxXOffsetProperty =
            AvaloniaProperty.RegisterDirect<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MaxXOffset), o => o.MaxXOffset);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if(change.Property == XScaleProperty)
            {
                float xStep = (float)ScopeWidth / _values.Length * XScale; // 計算每個資料點的寬度
                MaxXOffset = Math.Max(0.0f, _values.Length * xStep - (float)ScopeWidth); // 計算新的最大 XOffset
                CoerceValue(XOffsetProperty); // 重新計算 XOffset 的值，確保它在新的範圍內 [呼叫'coerce']
            }
        }

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

        public float StrokeWidth
        {
            get => GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        public float XScale
        {
            get => GetValue(XScaleProperty);
            set => SetValue(XScaleProperty, value);
        }

        public float YScale
        {
            get => GetValue(YScaleProperty);
            set => SetValue(YScaleProperty, value);
        }

        public float XOffset
        {
            get => GetValue(XOffsetProperty);
            set => SetValue(XOffsetProperty, value);
        }

        private float _maxXOffset = 0.0f;
        public float MaxXOffset
        {
            get => _maxXOffset;
            private set => SetAndRaise(MaxXOffsetProperty, ref _maxXOffset, value);
        }

        public void SetValues(float[] values)
        {
            _values = values;
            _version++;
            InvalidateVisual();
        }

        static SkiaDrawWaveformScopeUsrCtrl()
        {
            AffectsRender<SkiaDrawWaveformScopeUsrCtrl>(
                MinValueProperty, MaxValueProperty, 
                LineColorProperty, StrokeWidthProperty, 
                XScaleProperty, YScaleProperty, XOffsetProperty);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            YLableWith = 100;
        }

        private SkiaPen _skiaPen = null!;

        private float YLableWith { get; set; }
        private float ScopeWidth => (float)this.Bounds.Width - YLableWith;

        public override void Render(DrawingContext context)
        {
            var boundWith = this.Bounds.Width;
            var boundHeight = this.Bounds.Height;
            Debug.WriteLine($"Bounds {Bounds.ToString()}");

            if(_skiaPen is null)
            {
                _skiaPen = new SkiaPen(LineColor, StrokeWidth);
            }
            else if(!_skiaPen.Equals(LineColor, StrokeWidth))
            {
                _skiaPen.Dispose();
                _skiaPen = new SkiaPen(LineColor, StrokeWidth);
            }

            if (_values.Length < 2)
                return;

            double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

            SKPoint[] points = BuildSKPoints(_values, 
                (float)YLableWith, (float)0,
                (float)ScopeWidth, (float)boundHeight, 
                MaxValue, MinValue, scaling, XOffset, XScale, YScale);


            var scopeRect = new Rect(YLableWith, 0, ScopeWidth, boundHeight);
            context.Custom(new SkiaDrawLine(points, _skiaPen, scopeRect, _version));

        }

        private static SKPoint[] BuildSKPoints(ReadOnlySpan<float> values, float left, float top, float width, float higth,
            float maxValue, float minValue, double renderScaling, float xOffset= 0.0f, float xScale = 1.0f, float yScale = 1.0f)
        {
            int n = values.Length;
            if (n == 0) return Array.Empty<SKPoint>();

            float _dx = width / (n - 1) * xScale; // 每個資料點的寬度
            int drawN = (int)(width / _dx) + 1; // 計算需要的資料點數
            int startN = (int)(xOffset / _dx); // 計算起始資料點的索引

            int columns = Math.Max(1, (int)(width * renderScaling)); // 畫面寬度(DIP * 渲染縮放) => 需要的點數
            float xStep = width / columns; // 每個column的寬度
            float yRange = maxValue - minValue;
            float ScaleY(float v) => (1f - (v - minValue) / yRange) * higth; // 將值轉換為畫面高度的比例

            if (drawN <= columns * 2)
            {
                // 如果資料點數小於等於需要的點數，直接轉換為SKPoint
                var points = new SKPoint[drawN];
                float dx = width / (drawN - 1) * xScale; // 每個資料點的寬度

                int _dbgMinYIdx = -1;
                float _dbgMinYValue = float.MaxValue;

                for (int i = 0; i < drawN; i++)
                {
                    int yIndex = i + startN;
                    float yValue = ScaleY(values[yIndex]) * yScale + top;

                    if (yValue < _dbgMinYValue)
                    {
                        _dbgMinYValue = yValue;
                        _dbgMinYIdx = yIndex;
                    }
                    points[i] = new SKPoint(i * dx  + left - 500, yValue);
                }
                Debug.WriteLine($"Min Y Value: {_dbgMinYValue}, Min Y Index: {_dbgMinYIdx}");
                return points;
            }
            else
            {
                // 如果資料點數大於需要的點數，降採樣
                var points = new SKPoint[columns * 2];
                int pIdx = 0;
                for (int i = 0; i < columns; i++)
                {
                    // 計算每個column的資料範圍
                    int lo = (int)((long)i * n / columns);
                    int hi = (int)((long)(i + 1) * n / columns);
                    if (hi >= n) hi = n - 1;
                    // TODO: 處理降採樣邏輯
                    float min = values[lo], max = values[hi];
                    for(int j = lo; j < hi; j++)
                    {
                        float v = values[j];
                        if (v < min) { min = v; lo = j; }
                        if (v > max) { max = v; hi = j; }
                    }
                    float x = i * xStep * xScale + left - xOffset;
                    // 處理順序問題
                    if (min <= max)
                    {
                        points[pIdx++] = new SKPoint(x, ScaleY(min) * yScale + top);
                        points[pIdx++] = new SKPoint(x, ScaleY(max) * yScale + top);
                    }
                    else
                    {
                        points[pIdx++] = new SKPoint(x, ScaleY(max) * yScale + top);
                        points[pIdx++] = new SKPoint(x, ScaleY(min) * yScale + top);
                    }

                }
                return points;

            }
        }

        private class SkiaPen : IDisposable
        {
            bool isDisposed = false;
            private readonly Color _color;
            private readonly float _strokeWidth;
            private readonly SKPaint _sKPaint;
            public SkiaPen(Color color, float strokeWidth)
            {
                _color = color;
                _strokeWidth = strokeWidth;
                _sKPaint = new SKPaint
                {
                    Color = new SKColor(_color.R, _color.G, _color.B, _color.A),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = _strokeWidth,
                    IsAntialias = true,
                    StrokeJoin = SKStrokeJoin.Round,
                    StrokeCap = SKStrokeCap.Round,
                };
            }

            public SKPaint SKiaPaint => _sKPaint;

            public override bool Equals(object? obj)
            {
                return obj is SkiaPen other && _color.Equals(other._color) && _strokeWidth.Equals(other._strokeWidth);
            }

            public bool Equals(Color color, float strokeWidth)
            {
                return _color.Equals(color) && _strokeWidth.Equals(strokeWidth);
            }

            public void Dispose()
            {
                isDisposed = true;
            }
        }

        private class SkiaDrawLine : ICustomDrawOperation
        {
            public SkiaDrawLine(SKPoint[] points, SkiaPen skiaPen, Rect bounds, int version)
            {
                _points = points;
                _sKiaPen = skiaPen;
                Bounds = bounds;
                _version = version;
            }

            private readonly SKPoint[] _points;
            private readonly int _version;
            private readonly SkiaPen _sKiaPen;

            public Rect Bounds { get; }

            public void Dispose()
            {
                
            }

            public bool Equals(ICustomDrawOperation? other)
            {
                return other is SkiaDrawLine otherLine && _version == otherLine._version;
            }

            public bool HitTest(Point p)
            {
                return Bounds.Contains(p);
            }

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null || _points.Length < 2)
                    return;

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                canvas.Save();
                canvas.ClipRect(new SKRect((float)Bounds.Left, (float)Bounds.Top,
                    (float)Bounds.Right, (float)Bounds.Bottom));

                canvas.DrawPoints(SKPointMode.Polygon, _points, _sKiaPen.SKiaPaint);

                canvas.Restore();
            }
        }


        private class SkiaDrawGrid : ICustomDrawOperation
        {
            public Rect Bounds => throw new NotImplementedException();
            public void Dispose()
            {
                throw new NotImplementedException();
            }
            public bool Equals(ICustomDrawOperation? other)
            {
                throw new NotImplementedException();
            }
            public bool HitTest(Point p)
            {
                throw new NotImplementedException();
            }
            public void Render(ImmediateDrawingContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
