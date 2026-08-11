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

        public static readonly StyledProperty<ulong> CumulativePointsProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, ulong>(
                nameof(CumulativePoints), 0UL);

        public static readonly StyledProperty<float> MinValueProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MinValue), float.MaxValue);

        public static readonly StyledProperty<float> MaxValueProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, float>(
                nameof(MaxValue), float.MinValue);

        public static readonly StyledProperty<Color> WaveformLineColorProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, Color>(
                nameof(WaveformLineColor), Colors.DeepSkyBlue);

        public static readonly StyledProperty<IBrush?> MaxMinTextForegroundProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, IBrush?>(
                nameof(MaxMinTextForeground), Brushes.White);

        public static readonly StyledProperty<Color> GridLineColorProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, Color>(
                nameof(GridLineColor), Colors.White);

        public static readonly StyledProperty<Color> TimeAxisLineColorProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, Color>(
                nameof(TimeAxisLineColor), Colors.Red);

        public static readonly StyledProperty<Color> TimeAxisTextColorProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, Color>(
                nameof(TimeAxisTextColor), Colors.Red);

        public static readonly StyledProperty<IBrush?> CursorValueTextForegroundProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, IBrush?>(
                nameof(CursorValueTextForeground), Brushes.Red);

        public static readonly StyledProperty<IBrush?> CursorValueTextBackgroundProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, IBrush?>(
                nameof(CursorValueTextBackground), Brushes.Orange);

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

        public static readonly StyledProperty<double> SampleRateProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, double>(
                nameof(SampleRate), double.NaN);

        public static readonly StyledProperty<double> LabelIntervalProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, double>(
                nameof(LabelInterval), double.NaN);

        public static readonly StyledProperty<double> TickSpacingScaleProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, double>(
                nameof(TickSpacingScale), 1.0);

        public static readonly StyledProperty<IEnumerable?> ItemsProperty =
            AvaloniaProperty.Register<SkiaDrawWaveformScopeUsrCtrl, IEnumerable?>(
                nameof(Items), null);


        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property != SampleRateProperty && change.Property != LabelIntervalProperty && change.Property != TickSpacingScaleProperty)
                _skDrawWaveformLineVersion++;

            if (change.Property == XOffsetProperty || change.Property == XScaleProperty || change.Property == TickSpacingScaleProperty)
                _skDrawTimeAxisVersion++;

            if (change.Property == YScaleProperty || change.Property == YOffsetProperty || change.Property == MaxValueProperty || 
                change.Property == MinValueProperty || change.Property == MaxMinTextForegroundProperty || change.Property == GridLineColorProperty)
                _skDrawGridVersion++;

            if (change.Property == XScaleProperty)
            {
                int n = PointCount > 0 ? PointCount : _cacheValues.Length;
                float xStep = (float)(DrawWaveformWidth * XScale / (n - 1)); // 計算每個資料點的寬度
                MaxXOffset = Math.Max(0.0f, (n - 1) * xStep - (float)DrawWaveformWidth); // 計算新的最大 XOffset
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
            else if(change.Property == TickSpacingScaleProperty)
            {
                var value = (double)change.NewValue!;
                if (value < 0.0 || value > 1.0)
                {
                    SetCurrentValue(TickSpacingScaleProperty, Math.Clamp(value, 0.0, 1.0));
                }
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
            _skDrawWaveformLineVersion++;
            _skDrawTimeAxisVersion++;
        }

        public ulong CumulativePoints
        {
            get => GetValue(CumulativePointsProperty);
            set => SetValue(CumulativePointsProperty, value);
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

        public IBrush? MaxMinTextForeground
        {
            get => GetValue(MaxMinTextForegroundProperty);
            set => SetValue(MaxMinTextForegroundProperty, value);
        }

        public Color GridLineColor
        {
            get => GetValue(GridLineColorProperty);
            set => SetValue(GridLineColorProperty, value);
        }

        public Color TimeAxisLineColor
        {
            get => GetValue(TimeAxisLineColorProperty);
            set => SetValue(TimeAxisLineColorProperty, value);
        }

        public Color TimeAxisTextColor
        {
            get => GetValue(TimeAxisTextColorProperty);
            set => SetValue(TimeAxisTextColorProperty, value);
        }

        public IBrush? CursorValueTextForeground
        {
            get => GetValue(CursorValueTextForegroundProperty);
            set => SetValue(CursorValueTextForegroundProperty, value);
        }

        public IBrush? CursorValueTextBackground
        {
            get => GetValue(CursorValueTextBackgroundProperty);
            set => SetValue(CursorValueTextBackgroundProperty, value);
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

        public double SampleRate
        {
            get => GetValue(SampleRateProperty);
            set => SetValue(SampleRateProperty, value);
        }

        public double LabelInterval
        {
            get => GetValue(LabelIntervalProperty);
            set => SetValue(LabelIntervalProperty, value);
        }

        public double TickSpacingScale
        {
            get => GetValue(TickSpacingScaleProperty);
            set => SetValue(TickSpacingScaleProperty, value);
        }

        public IEnumerable? Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        static SkiaDrawWaveformScopeUsrCtrl()
        {
            AffectsRender<SkiaDrawWaveformScopeUsrCtrl>(
                CumulativePointsProperty,
                MinValueProperty, MaxValueProperty, 
                WaveformLineColorProperty, WaveformLineStrokeWidthProperty, 
                XScaleProperty, YScaleProperty, XOffsetProperty, YOffsetProperty,
                PointCountProperty, 
                SampleRateProperty, LabelIntervalProperty, TickSpacingScaleProperty, MaxMinTextForegroundProperty,
                TimeAxisLineColorProperty, GridLineColorProperty, TimeAxisTextColorProperty,
                ItemsProperty);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            DrawGridRectTop = 100;
            DrawGridRectLeft = 100;
            DrawTimeBarHeight = 100;
            DrawStatisticWidth = 100;
        }
        private float[] _cacheValues = Array.Empty<float>();
        private SkiaPaint _skiaWaveformLinePen = null!;
        private SkiaPaint _skiaGridLinePaint = null!;
        private SkiaPaint _skiaTimeAxisLinePaint = null!;

        private SkiaFont _timeAxisTickFont = null!;
        private SkiaPaint _timeAxisTextPaint = null!;


        private readonly float _drawWaveformMaxMinHeightMarginRate = 0.1f; // 10% margin
        private readonly float _drawWaveformMaxMinWidthMarginRate = 0.025f; // 2.5% margin
        private readonly float _drawWaveformLastPointPadding = 5.0f;
        private readonly float _drawCursorHighlightTextMarginLength = 5.0f;
        private float DrawGridRectTop { get; set; }
        private float DrawGridRectLeft { get; set; }
        private float DrawTimeBarHeight { get; set; }
        private float DrawStatisticWidth { get; set; }
        private float DrawGridWidth => (float)this.Bounds.Width - DrawGridRectLeft - DrawStatisticWidth;
        private float DrawGridHeight => (float)this.Bounds.Height - DrawGridRectTop - DrawTimeBarHeight;
        private float DrawGridBottom => DrawGridRectTop + DrawGridHeight;

        private float DrawWaveformLineTop => DrawGridRectTop + (DrawGridHeight * _drawWaveformMaxMinHeightMarginRate * 0.5f);
        private float DrawWaveformLineLeft => DrawGridRectLeft;
        private float DrawWaveformHeight => DrawGridHeight * (1 - _drawWaveformMaxMinHeightMarginRate);
        private float DrawWaveformWidth => DrawGridWidth - _drawWaveformLastPointPadding;

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
        private int _skDrawTimeAxisVersion;
        private SkiaDrawTimeAxis? _skiaDrawTimeAxis;

        private float _xStep = float.NaN;
        private bool _isDownSampling = false;

        private bool _showCursor = false;
        private Point _pointerPosition;

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            _showCursor = true;
            _pointerPosition = e.GetPosition(this);
            InvalidateVisual();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if(false == _showCursor)
            {
                return;
            }
            _pointerPosition = e.GetPosition(this);
            InvalidateVisual();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            _showCursor = false;
            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if(e.ClickCount == 2)
            {
                Debug.WriteLine("Pointer Double Clicked");
            }

            InvalidateVisual();
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            Debug.WriteLine("Pointer Capture Lost");

            InvalidateVisual();
        }

        private Stopwatch recordSpendTime = new Stopwatch();
        public override void Render(DrawingContext context)
        {
            recordSpendTime.Restart();
            DrawWaveform(context);
            var drawWaveformTime = recordSpendTime.Elapsed.TotalMilliseconds;
            recordSpendTime.Restart();
            DrawGrid(context);
            var drawGridTime = recordSpendTime.Elapsed.TotalMilliseconds;
            recordSpendTime.Restart();
            DrawTimeAxis(context);
            var drawTimeAxisTime = recordSpendTime.Elapsed.TotalMilliseconds;
            recordSpendTime.Restart();
            DrawCursor(context);
            var drawCursorTime = recordSpendTime.Elapsed.TotalMilliseconds;
            recordSpendTime.Restart();
            DrawFpsInfo(context);
            var drawFpsInfoTime = recordSpendTime.Elapsed.TotalMilliseconds;
            recordSpendTime.Stop();
            var totalDrawTime = drawWaveformTime + drawGridTime + drawTimeAxisTime + drawCursorTime + drawFpsInfoTime;
            Debug.WriteLine($"Draw Times - Waveform: {drawWaveformTime} ms, Grid: {drawGridTime} ms, TimeAxis: {drawTimeAxisTime} ms, Cursor: {drawCursorTime} ms, FpsInfo: {drawFpsInfoTime} ms, Total: {totalDrawTime} ms");
        }

        private void DrawWaveform(DrawingContext context)
        {
            var boundWith = this.Bounds.Width;
            var boundHeight = this.Bounds.Height;

            _skiaWaveformLinePen = _skiaWaveformLinePen.CompareOrGet(WaveformLineColor, WaveformLineStrokeWidth);

            if (_cacheValues.Length < 2)
                return;
            
            //if (PointCount > 0 && _cacheValues.Length > PointCount)
            //{
            //    _cacheValues = _cacheValues.AsSpan(_cacheValues.Length - PointCount).ToArray();
            //}

            if (_skiaDrawWaveformLine is null || _skiaDrawWaveformLine.Version != _skDrawWaveformLineVersion)
            {

                if(_drawScopeWaveformLineRect.IsEmpty || _drawScopeWaveformLineRect.Width != DrawWaveformWidth || _drawScopeWaveformLineRect.Height != DrawWaveformHeight)
                {
                    _drawScopeWaveformLineRect = new SKRect(DrawWaveformLineLeft, DrawWaveformLineTop, 0, 0);
                    _drawScopeWaveformLineRect.Size = new SKSize(DrawWaveformWidth, DrawWaveformHeight);
                }

                var (points, actualIndexes) = BuildScopeWaveformPoints(_cacheValues, PointCount,
                _drawScopeWaveformLineRect,
                MaxValue, MinValue, out _xStep, out _isDownSampling, XOffset, YOffset, XScale, YScale);
                _skiaDrawWaveformLine = new SkiaDrawWaveformLine(points, 
                    actualIndexes, _skiaWaveformLinePen, _drawScopeWaveformLineRect, _skDrawWaveformLineVersion);

            }

            context.Custom(_skiaDrawWaveformLine);

            if(_isDownSampling)
            {
                if(_downSamplingFormattedText is null)
                {
                    _downSamplingFormattedText = new FormattedText("#Downsampling mode", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Red);
                }
                context.DrawText(_downSamplingFormattedText, downSamplingDisplayPoint);
            }
        }

        private void DrawTimeAxis(DrawingContext context)
        {
            if (_skiaDrawWaveformLine is null) return;
            if (float.IsNaN(_xStep)) return;

            _skiaTimeAxisLinePaint = _skiaTimeAxisLinePaint.CompareOrGet(TimeAxisLineColor, 1.0f);
            _timeAxisTickFont = _timeAxisTickFont.CompareOrGet(SKTypeface.Default, 12, scaleX:1.0f);
            _timeAxisTextPaint = _timeAxisTextPaint.CompareOrGet(TimeAxisTextColor, 1.0f, SKPaintStyle.Fill);

            if (_skiaDrawTimeAxis is null || _skiaDrawTimeAxis.Version != _skDrawTimeAxisVersion)
            {
                float tickSpacingLineHeight = 5.0f;
                float tickTop = DrawGridBottom - tickSpacingLineHeight * 0.5f;
                float tickBottom = tickTop + tickSpacingLineHeight;

                float tickTextTop = tickBottom + 20.0f;
                float tickTextMargin = 5.0f;
                //if (double.IsNormal(SampleRate) && double.IsNormal(LabelInterval) && double.IsNormal(TickSpacing))
                if (double.IsNormal(TickSpacingScale) && TickSpacingScale != 0.0d)
                {
                    //int perTickSpacingCount = 50;// TickSpacing > _xStep ? (int)MathF.Ceiling((float)TickSpacing / _xStep) : 1;
                    //if(perTickSpacingCount == 0) perTickSpacingCount = 1;
                    List<TimeTextTick> timeTextTicks = new List<TimeTextTick>();
                    List<SKPoint> tickPointsList = new List<SKPoint>();
                    float firstPointX = _skiaDrawWaveformLine.Points.First().X;
                    float lastPointX = _skiaDrawWaveformLine.Points.Last().X;
                    int firstPointIndex = _skiaDrawWaveformLine.ActualIndexes.First();
                    int lastPointIndex = _skiaDrawWaveformLine.ActualIndexes.Last();
                    var points = _skiaDrawWaveformLine.Points;
                    var actualIndexes = _skiaDrawWaveformLine.ActualIndexes;
                    int totalPointCount = _skiaDrawWaveformLine.Points.Length;
                    float tickSpacingWidth = (float)(totalPointCount * _xStep) * (float)(1.0d - TickSpacingScale);

                    var startIndex = CumulativePoints - (ulong)_cacheValues.Length;
                    float lastTickXOffset = firstPointX;


                    var currTimeValue = (startIndex + (ulong)firstPointIndex) / SampleRate;
                    var currTimeTickText = $"{currTimeValue} S";
                    float currTickTextWidth = _timeAxisTickFont.MeasureTextWidth(currTimeTickText, out SKRect _, _timeAxisTextPaint.SKiaPaint);
                    float midLastTickTextWidth = currTickTextWidth * 0.5f;
                    // Ignore the first tick text, as it may overlap with the waveform line
                    float lastTickTextXOffset = firstPointX - midLastTickTextWidth + currTickTextWidth + tickTextMargin;

                    for (int pidx = 0; pidx < totalPointCount; pidx++)
                    {
                        var tickDiff = points[pidx].X - lastTickXOffset;
                        if (tickDiff >= tickSpacingWidth)
                        {
                            lastTickXOffset = points[pidx].X;
                            tickPointsList.Add(new SKPoint(lastTickXOffset, tickTop));
                            tickPointsList.Add(new SKPoint(lastTickXOffset, tickBottom));

                            currTimeValue = (startIndex + (ulong)actualIndexes[pidx]) / SampleRate;
                            currTimeTickText = $"{currTimeValue} S";
                            currTickTextWidth = _timeAxisTickFont.MeasureTextWidth(currTimeTickText, out SKRect _, _timeAxisTextPaint.SKiaPaint);
                            var currMidTickTextWidth = currTickTextWidth * 0.5f;
                            var tickTextDiff = points[pidx].X - currMidTickTextWidth - lastTickTextXOffset;

                            if(tickTextDiff > 0)
                            {
                                timeTextTicks.Add(new TimeTextTick { point = new SKPoint(points[pidx].X - currMidTickTextWidth, tickTextTop), Text = currTimeTickText });
                                lastTickTextXOffset = points[pidx].X + currMidTickTextWidth + tickTextMargin;
                            }
                        }
                    }

                    _skiaDrawTimeAxis = new SkiaDrawTimeAxis(
                        new SKRect(DrawGridRectLeft, tickTop, DrawGridRectLeft + DrawGridWidth, (float)this.Bounds.Bottom),
                        tickPointsList.ToArray(), _skiaTimeAxisLinePaint, 
                        _timeAxisTextPaint, _timeAxisTickFont, SKTextAlign.Left, timeTextTicks.ToArray(),
                        _skDrawTimeAxisVersion);
                }
                
            }
            if(_skiaDrawTimeAxis is not null && TickSpacingScale != 0.0d)
                context.Custom(_skiaDrawTimeAxis);
        }

        private void DrawGrid(DrawingContext context)
        {
            var boundWith = (float)this.Bounds.Width;
            var boundHeight = (float)this.Bounds.Height;

            var defaultHeight = DefaultDrawGridMinValueTop - DefaultDrawGridMaxValueTop;
            var actualHeight = defaultHeight * YScale;
            var actualMaxValueTop = DefaultDrawGridMaxValueTop - YOffset;
            var actualMinValueTop = actualMaxValueTop + actualHeight;

            var drawGridMaxMinValueScaleLineLeft = DrawGridRectLeft - DrawGridMaxMinValueScaleLineLength * 0.5f;

            _skiaGridLinePaint = _skiaGridLinePaint.CompareOrGet(GridLineColor, 1.0f);

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

                IBrush? maxMinTextForeground = MaxMinTextForeground;
                if (maxMinTextForeground is null)
                {
                    maxMinTextForeground = Brushes.White;
                }

                string maxValueText = $"{MaxValue} V";
                string minValueText = $"{MinValue} V";

                _maxValueDrawText = _maxValueDrawText.CompareOrGet(maxValueText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, maxMinTextForeground);
                _maxValueDrawText.Position = new Point(drawGridMaxMinValueScaleLineLeft - _maxValueDrawText.Width - _drawMaxMinValueTextMargin, actualMaxValueTop - _maxValueDrawText.MidHeight);

                _minValueDrawText = _minValueDrawText.CompareOrGet(minValueText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, maxMinTextForeground);
                _minValueDrawText.Position = new Point(drawGridMaxMinValueScaleLineLeft - _minValueDrawText.Width - _drawMaxMinValueTextMargin, actualMinValueTop - _minValueDrawText.MidHeight);

                _skiaDrawGrid = new SkiaDrawGrid(_drawRect, _drawScopeGridRect, _drawScopeWaveformLineRect,
                    actualMaxValueTop, actualMinValueTop, DrawGridMaxMinValueScaleLineLength,
                    _skiaGridLinePaint, _skDrawGridVersion);

                
            }

            context.Custom(_skiaDrawGrid);
            if(_maxValueDrawText is not null && _drawScopeWaveformLineRect.Top <= actualMaxValueTop)
                _maxValueDrawText?.Draw(context);
            if (_minValueDrawText is not null && _drawScopeWaveformLineRect.Bottom >= actualMinValueTop)
                _minValueDrawText?.Draw(context);
        }

        private void DrawCursor(DrawingContext context)
        {
            if(false == _showCursor)
                return;

            if (_skiaDrawWaveformLine is null) return;

            var gridRect = new Rect(DrawWaveformLineLeft, DrawWaveformLineTop, DrawWaveformWidth, DrawWaveformHeight);
            var currPointerPosition = _pointerPosition;
            
            if(false == gridRect.Contains(currPointerPosition))
                return;

            var points = _skiaDrawWaveformLine.Points;
            int index = FindNearestIndex(points, new SKPoint((float)currPointerPosition.X, (float)currPointerPosition.Y));

            if(index < 0)
            {
                return;    
            }

            SKPoint targetPoint = points[index];
            var cursorPen = new Pen(Brushes.Red, 1.0) { DashStyle = DashStyle.Dash };
            float midDHTL = _drawCursorHighlightTextMarginLength * 0.5f;
            
            var cursorValueTextForeground = CursorValueTextForeground;
            if (cursorValueTextForeground is null) cursorValueTextForeground = Brushes.Red;

            var cursorValueTextBackground = CursorValueTextBackground;
            if (cursorValueTextBackground is null) cursorValueTextBackground = Brushes.Orange;
            
            bool isPointInWaveformRect = true;
            // Draw vertical line
            if (targetPoint.X >= gridRect.Left && targetPoint.X <= gridRect.Right)
            { 
                context.DrawLine(cursorPen, new Point(targetPoint.X, gridRect.Top), new Point(targetPoint.X, DrawGridBottom));

                int actualIndex = _skiaDrawWaveformLine.ActualIndexes[index];
                DrawTextInfo valueText = null!;
                

                var startIndex = CumulativePoints - (ulong)_cacheValues.Length;
                if (SampleRate != 0.0d && double.IsPositive(SampleRate) && double.IsNormal(SampleRate))
                {
                    var timeValue = (startIndex + (ulong)actualIndex) / SampleRate;
                    valueText = new DrawTextInfo($"{timeValue} S", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, cursorValueTextForeground);
                }
                else
                {
                    valueText = new DrawTextInfo($"Idx:{actualIndex}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, cursorValueTextForeground);
                }
                
                valueText.Position = new Point(targetPoint.X - valueText.MidWidth, DrawGridBottom + _drawCursorHighlightTextMarginLength);

                context.DrawRectangle(cursorValueTextBackground, null, new Rect(valueText.Position.X - midDHTL, valueText.Position.Y , valueText.Width + _drawCursorHighlightTextMarginLength, valueText.Height + midDHTL));
                context.DrawText(valueText.FormattedText, valueText.Position);
            }
            else
                isPointInWaveformRect = false;

            // Draw horizontal line
            if (targetPoint.Y >= gridRect.Top && targetPoint.Y <= gridRect.Bottom)
            {
                context.DrawLine(cursorPen, new Point(gridRect.Left, targetPoint.Y), new Point(targetPoint.X, targetPoint.Y));
                var yValueRange = MaxValue - MinValue;
                var yHeight = gridRect.Height * YScale;
                var actualYHeight = YOffset + targetPoint.Y - gridRect.Top;
                var yHeightScale = (yHeight - actualYHeight) / yHeight;
                var yValue = MinValue + yHeightScale * yValueRange;
                DrawTextInfo valueText = new DrawTextInfo($"Val:{yValue:F2} V", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, cursorValueTextForeground);
                
                valueText.Position = new Point(gridRect.Left - valueText.Width - _drawCursorHighlightTextMarginLength, targetPoint.Y - valueText.MidHeight);

                context.DrawRectangle(cursorValueTextBackground, null, new Rect(valueText.Position.X - midDHTL, valueText.Position.Y, valueText.Width + midDHTL, valueText.Height));
                context.DrawText(valueText.FormattedText, valueText.Position);
            }
            else
                isPointInWaveformRect = false;

            if (true == isPointInWaveformRect)
            {
                // Draw circle at the intersection point
                var circleRadius = 4.0;
                context.DrawEllipse(Brushes.Red, cursorPen, new Point(targetPoint.X, targetPoint.Y), circleRadius, circleRadius);
            }
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

        private static int FindNearestIndex(SKPoint[] points, SKPoint target)
        {
            int idx = Array.BinarySearch(points, target,
        Comparer<SKPoint>.Create((a, b) => a.X.CompareTo(b.X)));

            if (idx >= 0) return idx; // X 精確命中

            // ref: https://jasson-chou-note.notion.site/C-Array-BinarySearch-Net-BinarySearch-3b1c3bd17136801aa46deb5d175c8062
            int upper = ~idx;
            if (upper == 0) return 0;
            if (upper >= points.Length) return points.Length - 1;

            int lower = upper - 1;
            return (target.X - points[lower].X) <= (points[upper].X - target.X)
                ? lower
                : upper;
        }

        private static (SKPoint[] skPoints, int[] actualIndexes) BuildScopeWaveformPoints(ReadOnlySpan<float> values, int pointCount, SKRect rect,
            float maxValue, float minValue, out float xStep, out bool isDownSampling, float xOffset= 0.0f, float yOffset = 0.0f, float xScale = 1.0f, float yScale = 1.0f)
        {
            int n = values.Length;
            isDownSampling = false;
            xStep = float.NaN;
            if (n == 0) 
            {
                return (Array.Empty<SKPoint>(), Array.Empty<int>());
            }
            
            float left = rect.Left;
            float top = rect.Top;
            float width = rect.Width;
            float higth = rect.Height;

            int canShowColumns = (int)Math.Ceiling(width);

            float yRange = maxValue - minValue;
            float yScaleFactor = (yRange != 0) ? (higth * yScale / yRange) : 1.0f; // 計算 Y 軸縮放因子
            float getYValue(float value) => top + (maxValue - value) * yScaleFactor - yOffset; // 計算 Y 軸座標

            xStep = 0.0f;

            if (pointCount > 0)
            {
                xStep = (float)(width * xScale / (pointCount - 1)); // 計算寬度，當固定點數時，使用 pointCount 來計算 xStep
            }
            else
            {
                xStep = (float)(width * xScale / (n - 1)); // 計算寬度，適合動態點數，使用 n 來計算 xStep
            }

            int startIndex = xOffset > 0 ? (int)(xOffset / xStep) : 0;
            int endIndex = Math.Min(n - 1, startIndex + (int)Math.Ceiling(width / xStep));
            int destCount = endIndex - startIndex + 1;


            if (destCount <= canShowColumns * 2)
            {
                //destCount = Math.Min(destCount, n - startIndex);
                SKPoint[] sKPoints = new SKPoint[destCount];
                int[] actualIndexes = new int[destCount];

                for (int i = 0; i < destCount && startIndex + i < n; i++)
                {
                    int valueIndex = startIndex + i;
                    float actualX = valueIndex * xStep - xOffset + left;
                    float y = getYValue(values[valueIndex]);
                    sKPoints[i] = new SKPoint(actualX, y);
                    actualIndexes[i] = valueIndex;
                }
                isDownSampling = false;
                return (sKPoints, actualIndexes);
            }
            else
            {
                // 採樣顯示，避免過多的點數 (min-max downsampling)
                int sampleRate = (int)Math.Ceiling((double)destCount / canShowColumns);
                SKPoint[] sKPoints = new SKPoint[canShowColumns * 2];
                int[] actualIndexes = new int[canShowColumns * 2];
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

                    float cLPosX = left + ((cLIdx - startIndex) * xStep);
                    float cHPosX = left + ((cHIdx - startIndex) * xStep);

                    if (cLIdx < cHIdx)
                    {
                        actualIndexes[sKPointIdx] = cLIdx;
                        sKPoints[sKPointIdx++] = new SKPoint(cLPosX, getYValue(cLValue));
                        actualIndexes[sKPointIdx] = cHIdx;
                        sKPoints[sKPointIdx++] = new SKPoint(cHPosX, getYValue(cHValue));
                    }
                    else
                    {
                        actualIndexes[sKPointIdx] = cHIdx;
                        sKPoints[sKPointIdx++] = new SKPoint(cHPosX, getYValue(cHValue));
                        actualIndexes[sKPointIdx] = cLIdx;
                        sKPoints[sKPointIdx++] = new SKPoint(cLPosX, getYValue(cLValue));
                    }

                }
                isDownSampling = true;
                return (sKPoints, actualIndexes);
            }

        }

        
        private class SkiaDrawWaveformLine : ICustomDrawOperation
        {
            public SkiaDrawWaveformLine(SKPoint[] points, int[] actualIndexes, SkiaPaint skiaPen, SKRect bounds, int version)
            {
                _points = points;
                _actualIndexes = actualIndexes;
                _sKiaPen = skiaPen;
                _bounds = bounds;
                Bounds = bounds.ToAvaloniaRect();
                _version = version;
            }

            private readonly SKPoint[] _points;
            private readonly int[] _actualIndexes;
            private readonly int _version;
            private readonly SkiaPaint? _sKiaPen;
            private readonly SKRect _bounds;
            
            public SKPoint[] Points => _points;

            public int[] ActualIndexes => _actualIndexes;

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

        private readonly record struct TimeTextTick(SKPoint point, string Text);
        private class SkiaDrawTimeAxis : ICustomDrawOperation
        {
            public SkiaDrawTimeAxis(SKRect bounds, SKPoint[] tickTopPoints, SkiaPaint skiaLinePaint,
                SkiaPaint skiaTextPaint, SkiaFont skiaTextFont, SKTextAlign textAlign, TimeTextTick[] timeTextTicks, int version)
            {
                _bounds = bounds;
                Bounds = bounds.ToAvaloniaRect();
                _tickPoints = tickTopPoints;
                _sKiaLinePaint = skiaLinePaint;
                _sKiaTextPaint = skiaTextPaint;
                _sKiaTextFont = skiaTextFont;
                _textAlign = textAlign;
                _timeTextTicks = timeTextTicks;
                _version = version;
            }


            public Rect Bounds { get; }
            public int Version => _version;
            private readonly SKRect _bounds;
            private readonly SKPoint[] _tickPoints;
            private readonly SkiaPaint _sKiaLinePaint;
            private readonly SkiaPaint _sKiaTextPaint;
            private readonly SkiaFont _sKiaTextFont;
            private readonly SKTextAlign _textAlign;
            private readonly TimeTextTick[] _timeTextTicks;
            private readonly int _version;

            private bool _isDisposed = false;
            public void Dispose()
            {
                _isDisposed = true;
            }

            public bool Equals(ICustomDrawOperation? other)
            {
                return other is SkiaDrawTimeAxis otherAxis && _version == otherAxis._version;
            }

            public bool HitTest(Point p)
            {
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

                if (_sKiaLinePaint is not null)
                {
                    canvas.DrawPoints(SKPointMode.Lines, _tickPoints, _sKiaLinePaint.SKiaPaint);
                }
                if (_sKiaTextPaint is not null && _sKiaTextFont is not null)
                {
                    foreach (var timeTextTick in _timeTextTicks)
                    {
                        canvas.DrawText(timeTextTick.Text, timeTextTick.point, _textAlign, _sKiaTextFont.SKiaFont, _sKiaTextPaint.SKiaPaint);
                    }
                }

                canvas.Restore();
            }
        }


        private class SkiaDrawGrid : ICustomDrawOperation
        {

            public SkiaDrawGrid(SKRect drawRect, SKRect drawGridRect, SKRect drawWaveformRect,
                float maxValueTop, float minValueTop, float scaleLineLength,
                SkiaPaint skiaPen, int version)
            {
                _version = version;
                _sKiaPen = skiaPen;
                _bounds = drawRect;
                _drawGridRect = drawGridRect;
                _drawWaveformRect = drawWaveformRect;
                _maxValueTop = maxValueTop;
                _minValueTop = minValueTop;
                _scaleLineLength = scaleLineLength;
                Bounds = _bounds.ToAvaloniaRect();
            }

            private readonly int _version;
            private readonly SkiaPaint? _sKiaPen;
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

        public double MidWidth => Width * 0.5;

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

    internal class SkiaPaint : IDisposable
    {
        bool isDisposed = false;
        internal readonly Color _color;
        internal readonly float _strokeWidth;
        internal readonly SKPaintStyle _sKPaintStyle;
        private readonly SKPaint _sKPaint;
        public SkiaPaint(Color color, float strokeWidth, SKPaintStyle sKPaintStyle = SKPaintStyle.Stroke)
        {
            _color = color;
            _strokeWidth = strokeWidth;
            _sKPaintStyle = sKPaintStyle;
            _sKPaint = new SKPaint
            {
                Color = _color.ToSKColor(),
                Style = _sKPaintStyle,
                StrokeWidth = _strokeWidth,
                IsAntialias = true,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
            };
        }

        public SKPaint SKiaPaint => _sKPaint;

        public bool Equals(Color color, float strokeWidth, SKPaintStyle sKPaintStyle = SKPaintStyle.Stroke)
        {
            return _color.Equals(color) && _strokeWidth.Equals(strokeWidth) && _sKPaintStyle == sKPaintStyle;
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

    internal class SkiaFont : IDisposable
    {
        bool isDisposed = false;
        internal readonly SKTypeface _typeface;
        internal readonly float _fontSize;
        internal readonly float _scaleX;
        internal readonly float _skewX;
        private readonly SKFont _sKFont;
        public SkiaFont(SKTypeface typeface, float fontSize, float scaleX = 12, float skewX = 0)
        {
            _typeface = typeface;
            _fontSize = fontSize;
            _scaleX = scaleX;
            _skewX = skewX;
            _sKFont = new SKFont(_typeface, _fontSize, _scaleX, _skewX);
        }
        public SKFont SKiaFont => _sKFont;
        public bool Equals(SKTypeface typeface, float fontSize, float scaleX = 12, float skewX = 0)
        {
            return _typeface.Equals(typeface) && _fontSize.Equals(fontSize) && _scaleX.Equals(scaleX) && _skewX.Equals(skewX);
        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                _sKFont.Dispose();
                _typeface.Dispose();
            }
        }
    }

    internal static class SkiaObjectExtensions
    {
        public static SkiaPaint CompareOrGet(this SkiaPaint? skiaPen, Color color, float strokeWidth, SKPaintStyle sKPaintStyle = SKPaintStyle.Stroke)
        {
            if (skiaPen is null || !skiaPen.Equals(color, strokeWidth, sKPaintStyle))
            {
                skiaPen?.Dispose();
                return new SkiaPaint(color, strokeWidth, sKPaintStyle);
            }
            return skiaPen;
        }

        public static float MeasureTextWidth(this SkiaFont skiaFont, string text, out SKRect bounds, SKPaint sKPaint = null!)
        {
            return skiaFont.SKiaFont.MeasureText(text, out bounds, sKPaint);
        }

        public static SkiaFont CompareOrGet(this SkiaFont? skiaFont, SKTypeface typeface, float fontSize, float scaleX = 12, float skewX = 0)
        {
            if (skiaFont is null || !skiaFont.Equals(typeface, fontSize, scaleX, skewX))
            {
                skiaFont?.Dispose();
                return new SkiaFont(typeface, fontSize, scaleX, skewX);
            }
            return skiaFont;
        }

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
