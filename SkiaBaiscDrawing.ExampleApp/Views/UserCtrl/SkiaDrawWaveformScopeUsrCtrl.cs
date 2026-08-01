using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Views.UserCtrl
{
    public class SkiaDrawWaveformScopeUsrCtrl : UserControl
    {

        private float[] _cacheValues = Array.Empty<float>();
        private int _skDrawLineVersion;

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

        public static readonly StyledProperty<float> YOffsetProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(YOffset), 0.0f, coerce: (o, value) =>
                {
                    var ctrl = (SkiaDrawWaveformScopeUsrCtrl)o;
                    return Math.Clamp(value, 0.0f, ctrl.MaxYOffset);
                });

        public static readonly DirectProperty<SkiaDrawWaveformScopeUsrCtrl, float> MaxYOffsetProperty =
            AvaloniaProperty.RegisterDirect<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MaxYOffset), o => o.MaxYOffset);

        public static readonly StyledProperty<int> PointCountProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, int>(
                nameof(PointCount), 2048);

        public static readonly StyledProperty<IEnumerable?> ItemsProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, IEnumerable?>(
                nameof(Items), null);


        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if(change.Property == XScaleProperty)
            {
                double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
                float xStep = (float)(ScopeWidth * XScale * scaling / (_cacheValues.Length - 1)); // 計算每個資料點的寬度
                MaxXOffset = Math.Max(0.0f, (_cacheValues.Length - 1) * xStep - (float)ScopeWidth); // 計算新的最大 XOffset
                CoerceValue(XOffsetProperty); // 重新計算 XOffset 的值，確保它在新的範圍內 [呼叫'coerce']
            }
            else if(change.Property == YScaleProperty)
            {
                MaxYOffset = YScale == 1.0f ? 0.0f : ScopeHeight * YScale - ScopeHeight; // 計算新的最大 YOffset
                CoerceValue(YOffsetProperty); // 重新計算 YOffset 的值，確保它在新的範圍內 [呼叫'coerce']
            }
            else if(change.Property == ItemsProperty)
            {
                if (_itemsNotifyCollectionChanged is not null)
                {
                    _itemsNotifyCollectionChanged.CollectionChanged -= Items_CollectionChanged;
                    _itemsNotifyCollectionChanged = null;
                }
                if (Items is INotifyCollectionChanged notifyCollection)
                {
                    _itemsNotifyCollectionChanged = notifyCollection;
                    _itemsNotifyCollectionChanged.CollectionChanged += Items_CollectionChanged;
                }
                RebuildItemsCache();
            }
        }

        private INotifyCollectionChanged? _itemsNotifyCollectionChanged;
        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildItemsCache();
            InvalidateVisual();
        }

        private void RebuildItemsCache()
        {
            _cacheValues = Items switch
            {
                IEnumerable<float> floatItems => floatItems.ToArray(),
                IEnumerable<double> doubleItems => doubleItems.Select(d => (float)d).ToArray(),
                _ => Array.Empty<float>(),
            };
            _skDrawLineVersion++;
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

        public float YOffset
        {
            get => GetValue(YOffsetProperty);
            set => SetValue(YOffsetProperty, value);
        }

        private float _maxYOffset = 0.0f;
        public float MaxYOffset
        {
            get => _maxYOffset;
            private set => SetAndRaise(MaxYOffsetProperty, ref _maxYOffset, value);
        }

        public int PointCount
        {
            get => GetValue(PointCountProperty);
            set => SetValue(PointCountProperty, value);
        }

        public IEnumerable? Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public void SetItems(float[] values) => Items = values;

        static SkiaDrawWaveformScopeUsrCtrl()
        {
            AffectsRender<SkiaDrawWaveformScopeUsrCtrl>(
                MinValueProperty, MaxValueProperty, 
                LineColorProperty, StrokeWidthProperty, 
                XScaleProperty, YScaleProperty, XOffsetProperty, YOffsetProperty,
                PointCountProperty, ItemsProperty);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            YLableWith = 100;
            XLableHeight = 100;
        }

        private SkiaPen _skiaPen = null!;

        private float YLableWith { get; set; }
        private float XLableHeight { get; set; }
        private float ScopeWidth => (float)this.Bounds.Width - YLableWith;
        private float ScopeHeight => (float)this.Bounds.Height - XLableHeight;

        private Stopwatch _fpsStopwatch = new Stopwatch();
        private int _fpsAccumulatedFrames = 0;
        private int _fpsUpdateCount = 0;
        private FormattedText? _fpsFormattedText;
        private readonly Point fpsDisplayPoint = new Point(10, 10);

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
        }

        public override void Render(DrawingContext context)
        {

            DrawWaveform(context);

            DrawFpsInfo(context);
        }

        private void DrawWaveform(DrawingContext context)
        {
            var boundWith = this.Bounds.Width;
            var boundHeight = this.Bounds.Height;

            if (_skiaPen is null)
            {
                _skiaPen = new SkiaPen(LineColor, StrokeWidth);
            }
            else if (!_skiaPen.Equals(LineColor, StrokeWidth))
            {
                _skiaPen.Dispose();
                _skiaPen = new SkiaPen(LineColor, StrokeWidth);
            }

            if (_cacheValues.Length < 2)
                return;

            double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

            if (PointCount > 0 && _cacheValues.Length > PointCount)
            {
                _cacheValues = _cacheValues.AsSpan(_cacheValues.Length - PointCount).ToArray();
            }

            SKPoint[] points = BuildSKPoints(_cacheValues, PointCount,
                (float)YLableWith, (float)0,
                (float)ScopeWidth, (float)ScopeHeight,
                MaxValue, MinValue, scaling, XOffset, YOffset, XScale, YScale);


            var scopeRect = new Rect(YLableWith, 0, ScopeWidth, ScopeHeight);
            context.Custom(new SkiaDrawLine(points, _skiaPen, scopeRect, _skDrawLineVersion));
        }

        private void DrawFpsInfo(DrawingContext context)
        {
            

            int Fps = 0;
            if (_fpsStopwatch.Elapsed.TotalMilliseconds is double elapsedMs && elapsedMs > 0.0)
            {
                Fps = (int)(1000.0 / elapsedMs);
            }
            _fpsAccumulatedFrames += Fps;

            if (_fpsUpdateCount++ > 10)
            {
                Fps = _fpsAccumulatedFrames / _fpsUpdateCount;
                _fpsFormattedText = new FormattedText($"FPS: {Fps}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 16, Brushes.White);
                _fpsUpdateCount = 0;
                _fpsAccumulatedFrames = 0;
            }

            if(_fpsFormattedText is not null)
            {
                context.DrawText(_fpsFormattedText, fpsDisplayPoint);
            }


            _fpsStopwatch.Restart();
        }

        private static SKPoint[] BuildSKPoints(ReadOnlySpan<float> values, int pointCount, float left, float top, float width, float higth,
            float maxValue, float minValue, double renderScaling, float xOffset= 0.0f, float yOffset = 0.0f, float xScale = 1.0f, float yScale = 1.0f)
        {
            int n = values.Length;
            if (n == 0) return Array.Empty<SKPoint>();
            
            int canShowColumns = (int)Math.Ceiling(width * renderScaling);

            float yRange = maxValue - minValue;
            float yScaleFactor = (yRange != 0) ? (higth * yScale / yRange) : 1.0f; // 計算 Y 軸縮放因子
            float getYValue(float value) => top + (maxValue - value) * yScaleFactor - yOffset; // 計算 Y 軸座標

            float xStep = 0.0f;

            if (pointCount > 0)
            {
                xStep = (float)(width * xScale * renderScaling / (pointCount - 1)); // 計算每個資料點的寬度
            }
            else
            {
                xStep = (float)(width * xScale * renderScaling / (n - 1)); // 計算每個資料點的寬度
            }

            int startIndex = xOffset > 0 ? (int)(xOffset / xStep) : 0;
            int destCount = (int)(canShowColumns / xStep);
            

            if (destCount <= canShowColumns * 2)
            {
                destCount = Math.Min(destCount, n - startIndex);
                SKPoint[] sKPoints = new SKPoint[destCount];
                float x = left;
                for (int i = 0; i < destCount && startIndex + i < n; i++)
                {
                    int valueIndex = startIndex + i;
                    float y = getYValue(values[valueIndex]);
                    sKPoints[i] = new SKPoint(x, y);
                    x += xStep;
                }
                return sKPoints;
            }
            else
            {
                // 採樣顯示，避免過多的點數 (min-max downsampling)
                int sampleRate = (int)Math.Ceiling((double)destCount / canShowColumns);
                SKPoint[] sKPoints = new SKPoint[canShowColumns * 2];
                int sKPointIdx = 0;
                for (int i = 0; i < canShowColumns; i++)
                {
                    int cLIdx = Math.Min(startIndex + (i * sampleRate), n - 1);
                    int cHIdx = Math.Min(cLIdx + sampleRate - 1, n - 1);

                    float cLValue = values[cLIdx];
                    float cHValue = values[cHIdx];

                    for (int j = cLIdx; j <= cHIdx; j++)
                    {
                        if (cLValue > values[j])
                        {
                            cLValue = values[j];
                            cLIdx = j;
                        }

                        if (cHValue < values[j])
                        {
                            cHValue = values[j];
                            cHIdx = j;
                        }
                    }

                    if (cLIdx < cHIdx)
                    {
                        sKPoints[sKPointIdx++] = new SKPoint(left + (cLIdx * xStep), getYValue(cLValue));
                        sKPoints[sKPointIdx++] = new SKPoint(left + (cHIdx * xStep), getYValue(cHValue));
                    }
                    else
                    {
                        sKPoints[sKPointIdx++] = new SKPoint(left + (cHIdx * xStep), getYValue(cHValue));
                        sKPoints[sKPointIdx++] = new SKPoint(left + (cLIdx * xStep), getYValue(cLValue));
                    }

                }

                return sKPoints;
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
