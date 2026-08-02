using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Views.UserCtrl
{
    public class SkiaDrawWaveformScopeUsrCtrl : UserControl
    {

        

        public static readonly StyledProperty<float> MinValueProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MinValue), float.MaxValue);

        public static readonly StyledProperty<float> MaxValueProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MaxValue), float.MinValue);

        public static readonly StyledProperty<Color> WaveformLineColorProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, Color>(
                nameof(WaveformLineColor), Colors.DeepSkyBlue);

        public static readonly StyledProperty<float> WaveformLineStrokeWidthProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(WaveformLineStrokeWidth), 1.0f);

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
                float xStep = (float)(DrawGridWidth * XScale * scaling / (_cacheValues.Length - 1)); // 計算每個資料點的寬度
                MaxXOffset = Math.Max(0.0f, (_cacheValues.Length - 1) * xStep - (float)DrawGridWidth); // 計算新的最大 XOffset
                CoerceValue(XOffsetProperty); // 重新計算 XOffset 的值，確保它在新的範圍內 [呼叫'coerce']
            }
            else if(change.Property == YScaleProperty)
            {
                var defaultHeight = DefaultDrawGridMinValueTop - DefaultDrawGridMaxValueTop;
                MaxYOffset = YScale == 1.0f ? 0.0f : defaultHeight * YScale - defaultHeight; // 計算新的最大 YOffset
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
            _skDrawWaveformLineVersion++;
            _skDrawGridVersion++;

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
            _skDrawWaveformLineVersion++;
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

        public Color WaveformLineColor
        {
            get => GetValue(WaveformLineColorProperty);
            set => SetValue(WaveformLineColorProperty, value);
        }

        public float WaveformLineStrokeWidth
        {
            get => GetValue(WaveformLineStrokeWidthProperty);
            set => SetValue(WaveformLineStrokeWidthProperty, value);
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

        static SkiaDrawWaveformScopeUsrCtrl()
        {
            AffectsRender<SkiaDrawWaveformScopeUsrCtrl>(
                MinValueProperty, MaxValueProperty, 
                WaveformLineColorProperty, WaveformLineStrokeWidthProperty, 
                XScaleProperty, YScaleProperty, XOffsetProperty, YOffsetProperty,
                PointCountProperty, ItemsProperty);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            DrawGridRectTop = 100;
            DrawGridRectLeft = 100;
            DrawTimeBarHeight = 100;
        }
        private float[] _cacheValues = Array.Empty<float>();
        private SkiaPen _skiaWaveformLinePen = null!;
        private SkiaPen _skiaGridLinePen = null!;


        private readonly float _drawWaveformMaxMinHeightMarginRate = 0.1f; // 10% margin
        private readonly float _drawWaveformMaxMinWidthMarginRate = 0.025f; // 2.5% margin
        private float DrawGridRectTop { get; set; }
        private float DrawGridRectLeft { get; set; }
        private float DrawTimeBarHeight { get; set; }
        private float DrawGridWidth => (float)this.Bounds.Width - DrawGridRectLeft;
        private float DrawGridHeight => (float)this.Bounds.Height - DrawGridRectTop - DrawTimeBarHeight;

        private float DrawWaveformLineTop => DrawGridRectTop + (DrawGridHeight * _drawWaveformMaxMinHeightMarginRate * 0.5f);
        private float DrawWaveformLineLeft => DrawGridRectLeft;
        private float DrawWaveformHeight => DrawGridHeight * (1 - _drawWaveformMaxMinHeightMarginRate);
        private float DrawWaveformWidth => DrawGridWidth;

        private float DefaultDrawGridMaxValueTop => DrawWaveformLineTop;
        private float DefaultDrawGridMinValueTop => DefaultDrawGridMaxValueTop + DrawWaveformHeight;

        private float DrawGridMaxMinValueScaleLineLength => DrawGridWidth * _drawWaveformMaxMinWidthMarginRate;

        private Stopwatch _fpsStopwatch = new Stopwatch();
        private int _fpsAccumulatedFrames = 0;
        private int _fpsUpdateCount = 0;
        private FormattedText? _fpsFormattedText;
        private readonly Point fpsDisplayPoint = new Point(10, 10);
        private FormattedText? _downSamplingFormattedText;
        private readonly Point downSamplingDisplayPoint = new Point(10, 30);

        private readonly float _drawMaxMinValueTextMargin = 5.0f; // Margin between the max/min value text and the scale line
        private DrawTextInfo? _maxValueDrawText;
        private DrawTextInfo? _minValueDrawText;


        private SKRect _drawRect = SKRect.Empty;
        private SKRect _drawScopeWaveformLineRect = SKRect.Empty;
        private SKRect _drawScopeGridRect = SKRect.Empty;
        private int _skDrawWaveformLineVersion;
        private SkiaDrawWaveformLine? _skiaDrawWaveformLine;
        private int _skDrawGridVersion;
        private SkiaDrawGrid? _skiaDrawGrid;

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
        }

        public override void Render(DrawingContext context)
        {

            DrawWaveform(context);

            DrawGrid(context);

            DrawFpsInfo(context);
        }

        private void DrawWaveform(DrawingContext context)
        {
            var boundWith = this.Bounds.Width;
            var boundHeight = this.Bounds.Height;

            _skiaWaveformLinePen = _skiaWaveformLinePen.CompareOrGet(WaveformLineColor, WaveformLineStrokeWidth);

            if (_cacheValues.Length < 2)
                return;

            double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            bool isDownSampling = false;
            if (PointCount > 0 && _cacheValues.Length > PointCount)
            {
                _cacheValues = _cacheValues.AsSpan(_cacheValues.Length - PointCount).ToArray();
            }

            if (_skiaDrawWaveformLine is null || _skiaDrawWaveformLine.Version != _skDrawWaveformLineVersion)
            {

                if(_drawScopeWaveformLineRect.IsEmpty || _drawScopeWaveformLineRect.Width != DrawWaveformWidth || _drawScopeWaveformLineRect.Height != DrawWaveformHeight)
                {
                    _drawScopeWaveformLineRect = new SKRect(DrawWaveformLineLeft, DrawWaveformLineTop, 0, 0);
                    _drawScopeWaveformLineRect.Size = new SKSize(DrawWaveformWidth, DrawWaveformHeight);
                }

                SKPoint[] points = BuildScopeWaveformPoints(_cacheValues, PointCount,
                _drawScopeWaveformLineRect,
                MaxValue, MinValue, scaling, out isDownSampling, XOffset, YOffset, XScale, YScale);
                _skiaDrawWaveformLine = new SkiaDrawWaveformLine(points, _skiaWaveformLinePen, _drawScopeWaveformLineRect, _skDrawWaveformLineVersion);

            }

            context.Custom(_skiaDrawWaveformLine);

            if(isDownSampling)
            {
                if(_downSamplingFormattedText is null)
                {
                    _downSamplingFormattedText = new FormattedText("Downsampling...", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Red);
                }
                context.DrawText(_downSamplingFormattedText, downSamplingDisplayPoint);
            }
        }

        private void DrawGrid(DrawingContext context)
        {
            var boundWith = (float)this.Bounds.Width;
            var boundHeight = (float)this.Bounds.Height;
            double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

            var defaultHeight = DefaultDrawGridMinValueTop - DefaultDrawGridMaxValueTop;
            var actualHeight = defaultHeight * YScale;
            var actualMaxValueTop = DefaultDrawGridMaxValueTop - YOffset;
            var actualMinValueTop = actualMaxValueTop + actualHeight;

            var drawGridMaxMinValueScaleLineLeft = DrawGridRectLeft - DrawGridMaxMinValueScaleLineLength * 0.5f;

            _skiaGridLinePen = _skiaGridLinePen.CompareOrGet(Colors.Gray, 1.0f);

            if (_skiaDrawGrid is null || _skiaDrawGrid.Version != _skDrawGridVersion)
            {
                if(_drawRect.IsEmpty || _drawRect.Width != DrawGridWidth || _drawRect.Height != DrawGridHeight)
                {
                    _drawRect = new SKRect(0, 0, 0, 0);
                    _drawRect.Size = new SKSize(boundWith, boundHeight);
                }

                if(_drawScopeGridRect.IsEmpty || _drawScopeGridRect.Width != DrawGridWidth || _drawScopeGridRect.Height != DrawGridHeight)
                {
                    _drawScopeGridRect = new SKRect(DrawGridRectLeft, DrawGridRectTop, 0, 0);
                    _drawScopeGridRect.Size = new SKSize(DrawGridWidth, DrawGridHeight);
                }

                if (_drawScopeWaveformLineRect.IsEmpty || _drawScopeWaveformLineRect.Width != DrawWaveformWidth || _drawScopeWaveformLineRect.Height != DrawWaveformHeight)
                {
                    _drawScopeWaveformLineRect = new SKRect(DrawWaveformLineLeft, DrawWaveformLineTop, 0, 0);
                    _drawScopeWaveformLineRect.Size = new SKSize(DrawWaveformWidth, DrawWaveformHeight);
                }


                string maxValueText = $"{MaxValue} V";
                string minValueText = $"{MinValue} V";

                _maxValueDrawText = _maxValueDrawText.CompareOrGet(maxValueText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.White);
                _maxValueDrawText.Position = new Point(drawGridMaxMinValueScaleLineLeft - _maxValueDrawText.Width - _drawMaxMinValueTextMargin, actualMaxValueTop - _maxValueDrawText.MidHeight);

                _minValueDrawText = _minValueDrawText.CompareOrGet(minValueText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.White);
                _minValueDrawText.Position = new Point(drawGridMaxMinValueScaleLineLeft - _minValueDrawText.Width - _drawMaxMinValueTextMargin, actualMinValueTop - _minValueDrawText.MidHeight);

                _skiaDrawGrid = new SkiaDrawGrid(_drawRect, _drawScopeGridRect, _drawScopeWaveformLineRect,
                    actualMaxValueTop, actualMinValueTop, DrawGridMaxMinValueScaleLineLength,
                    _skiaGridLinePen, _skDrawGridVersion);

                
            }

            context.Custom(_skiaDrawGrid);
            if(_maxValueDrawText is not null && _drawScopeWaveformLineRect.Top <= actualMaxValueTop)
                _maxValueDrawText?.Draw(context);
            if (_minValueDrawText is not null && _drawScopeWaveformLineRect.Bottom >= actualMinValueTop)
                _minValueDrawText?.Draw(context);
        }

        private void DrawFpsInfo(DrawingContext context)
        {
            

            int Fps = 0;
            if (_fpsStopwatch.Elapsed.TotalMilliseconds is double elapsedMs && elapsedMs > 0.0)
            {
                Fps = (int)(1000.0 / elapsedMs);
            }
            _fpsAccumulatedFrames += Fps;

            if (_fpsUpdateCount++ >= 10)
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

        private static SKPoint[] BuildScopeWaveformPoints(ReadOnlySpan<float> values, int pointCount, SKRect rect,
            float maxValue, float minValue, double renderScaling, out bool isDownSampling, float xOffset= 0.0f, float yOffset = 0.0f, float xScale = 1.0f, float yScale = 1.0f)
        {
            int n = values.Length;
            isDownSampling = false;
            if (n == 0) 
            {
                return Array.Empty<SKPoint>();
            }
            
            float left = rect.Left;
            float top = rect.Top;
            float width = rect.Width;
            float higth = rect.Height;

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
                isDownSampling = false;
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
                isDownSampling = true;
                return sKPoints;
            }

        }

        
        private class SkiaDrawWaveformLine : ICustomDrawOperation
        {
            public SkiaDrawWaveformLine(SKPoint[] points, SkiaPen skiaPen, SKRect bounds, int version)
            {
                _points = points;
                _sKiaPen = skiaPen;
                _bounds = bounds;
                Bounds = new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                _version = version;
            }

            private readonly SKPoint[] _points;
            private readonly int _version;
            private readonly SkiaPen? _sKiaPen;
            private readonly SKRect _bounds;
            public int Version => _version;

            public Rect Bounds { get; }

            public void Dispose()
            {
                
            }

            public bool Equals(ICustomDrawOperation? other)
            {
                return other is SkiaDrawWaveformLine otherLine && _version == otherLine._version;
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
                canvas.ClipRect(_bounds);

                if(_sKiaPen is not null)
                    canvas.DrawPoints(SKPointMode.Polygon, _points, _sKiaPen.SKiaPaint);

                canvas.Restore();
            }
        }


        private class SkiaDrawGrid : ICustomDrawOperation
        {

            public SkiaDrawGrid(SKRect drawRect, SKRect drawGridRect, SKRect drawWaveformRect,
                float maxValueTop, float minValueTop, float scaleLineLength,
                SkiaPen skiaPen, int version)
            {
                _version = version;
                _sKiaPen = skiaPen;
                _bounds = drawRect;
                _drawGridRect = drawGridRect;
                _drawWaveformRect = drawWaveformRect;
                _maxValueTop = maxValueTop;
                _minValueTop = minValueTop;
                _scaleLineLength = scaleLineLength;
                Bounds = new Rect(_bounds.Left, _bounds.Top, _bounds.Width, _bounds.Height);
            }

            private readonly int _version;
            private readonly SkiaPen? _sKiaPen;
            private readonly SKRect _bounds;
            private readonly SKRect _drawGridRect;
            private readonly SKRect _drawWaveformRect;
            private readonly float _maxValueTop;
            private readonly float _minValueTop;
            private readonly float _scaleLineLength;
            public int Version => _version;
            public Rect Bounds { get; }
            public void Dispose()
            {
                //throw new NotImplementedException();
            }
            public bool Equals(ICustomDrawOperation? other)
            {
                //throw new NotImplementedException();
                return other is SkiaDrawGrid otherGrid && _version == otherGrid._version;
            }
            public bool HitTest(Point p)
            {
                //throw new NotImplementedException();
                return Bounds.Contains(p);
            }
            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;
                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                canvas.Save();
                canvas.ClipRect(_bounds);

                if(_sKiaPen is not null)
                {
                    canvas.DrawLine(_drawGridRect.Left, _drawGridRect.Top, _drawGridRect.Left, _drawGridRect.Bottom, _sKiaPen.SKiaPaint);
                    canvas.DrawLine(_drawGridRect.Left, _drawGridRect.Bottom,_drawGridRect.Right, _drawGridRect.Bottom, _sKiaPen.SKiaPaint);
                    float midScaleLineLength = _scaleLineLength * 0.5f;

                    if(_drawWaveformRect.Top <= _maxValueTop)
                        canvas.DrawLine(_drawGridRect.Left - midScaleLineLength, _maxValueTop, _drawGridRect.Left + midScaleLineLength, _maxValueTop, _sKiaPen.SKiaPaint);

                    if(_drawWaveformRect.Bottom >= _minValueTop)
                        canvas.DrawLine(_drawGridRect.Left - midScaleLineLength, _minValueTop, _drawGridRect.Left + midScaleLineLength, _minValueTop, _sKiaPen.SKiaPaint);
                    
                }

                canvas.Restore();
            }
        }
    }

    internal class DrawTextInfo
    {
        public DrawTextInfo(string text, CultureInfo cultureInfo, FlowDirection flowDirection, Typeface typeface, double fontSize, IBrush brush) 
        { 
            Text = text;
            CultureInfo = cultureInfo;
            FlowDirection = flowDirection;
            Typeface = typeface;
            FontSize = fontSize;
            Brush = brush;
            FormattedText = new FormattedText(Text, CultureInfo, FlowDirection, Typeface, FontSize, Brush);
        }

        public string Text { get; }

        public CultureInfo CultureInfo { get; } = CultureInfo.InvariantCulture;

        public FlowDirection FlowDirection { get; } = FlowDirection.LeftToRight;

        public Typeface Typeface { get; }

        public double FontSize { get; } = 12.0;

        public IBrush? Brush { get; } = Brushes.White;

        public FormattedText FormattedText { get; }

        public double Height => FormattedText.Height;

        public double MidHeight => Height * 0.5;

        public double Width => FormattedText.Width;

        public Point Position { get; set; }

        public bool Equals(string text, CultureInfo cultureInfo, FlowDirection flowDirection, Typeface typeface, double fontSize, IBrush brush)
        {
            return Text == text &&
                CultureInfo.Equals(cultureInfo) &&
                FlowDirection == flowDirection &&
                Typeface.Equals(typeface) &&
                FontSize.Equals(fontSize) &&
                Brush?.Equals(brush) == true;
        }

        public void Draw(DrawingContext context)
        {
            context.DrawText(FormattedText, Position);
        }

    }

    internal class SkiaPen : IDisposable
    {
        bool isDisposed = false;
        internal readonly Color _color;
        internal readonly float _strokeWidth;
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

        public bool Equals(Color color, float strokeWidth)
        {
            return _color.Equals(color) && _strokeWidth.Equals(strokeWidth);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                _sKPaint.Dispose();
            }
        }
    }

    internal static class SkiaPenExtensions
    {
        public static SkiaPen CompareOrGet(this SkiaPen? skiaPen, Color color, float strokeWidth)
        {
            if (skiaPen is null || !skiaPen.Equals(color, strokeWidth))
            {
                skiaPen?.Dispose();
                return new SkiaPen(color, strokeWidth);
            }
            return skiaPen;
        }
    }

    internal static class DrawTextInfoExtensions
    {
        public static DrawTextInfo CompareOrGet(this DrawTextInfo? drawTextInfo, string text, CultureInfo cultureInfo, FlowDirection flowDirection, Typeface typeface, double fontSize, IBrush brush)
        {
            if (drawTextInfo is null || !drawTextInfo.Equals(text, cultureInfo, flowDirection, typeface, fontSize, brush))
            {
                return new DrawTextInfo(text, cultureInfo, flowDirection, typeface, fontSize, brush);
            }
            return drawTextInfo;
        }
    }

}
