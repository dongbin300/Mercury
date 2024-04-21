using ChartViewerPrism.Apis;

using Mercury.Charts;

using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ChartViewerPrism.Views
{
	public class CandleChartControl : SKElement
	{
		private readonly SKFont CandleInfoFont = new(SKTypeface.FromFamilyName("Meiryo UI"), 12);
		private readonly SKPaint CandleInfoPaint = new() { Color = SKColors.White };
		private readonly SKPaint HorizontalLinePointerPaint = new() { Color = SKColors.Silver };
		private static readonly SKColor LongColor = new(59, 207, 134);
		private static readonly SKColor LongVolumeColor = new(59, 207, 134, 64);
		private static readonly SKColor ShortColor = new(237, 49, 97);
		private static readonly SKColor ShortVolumeColor = new(237, 49, 97, 64);
		private readonly SKPaint LongPaint = new() { Color = LongColor };
		private readonly SKPaint LongVolumePaint = new() { Color = LongVolumeColor };
		private readonly SKPaint ShortPaint = new() { Color = ShortColor };
		private readonly SKPaint ShortVolumePaint = new() { Color = ShortVolumeColor };
		private readonly SKPaint CandlePointerPaint = new() { Color = new SKColor(255, 255, 255, 32) };
		private readonly int CandleTopBottomMargin = 10;
		private int ChartCount => Charts.Count;
		public float CurrentMouseX;
		public float CurrentMouseY;

		float LiveActualWidth;
		float LiveActualHeight;
		float LiveActualItemFullWidth => LiveActualWidth / ChartCount;
		float LiveActualItemMargin => LiveActualItemFullWidth * 0.2f;

		public static readonly DependencyProperty ChartsProperty =
		   DependencyProperty.Register("Charts", typeof(ObservableCollection<ChartInfo>), typeof(CandleChartControl), new PropertyMetadata(new ObservableCollection<ChartInfo>(), OnChartsPropertyChanged));

		private static void OnChartsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = d as CandleChartControl;
			control.InvalidateVisual();
		}

		public ObservableCollection<ChartInfo> Charts
		{
			get
			{
				return (ObservableCollection<ChartInfo>)GetValue(ChartsProperty);
			}
			set
			{
				SetValue(ChartsProperty, value);
			}
		}

		public CandleChartControl()
		{
			PaintSurface += OnPaintSurface;
			MouseMove += OnMouseMove;
		}

		private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			try
			{
				var cursorPosition = WinApi.GetCursorPosition();
				var x = (float)cursorPosition.X - (float)PointToScreen(new Point(0, 0)).X;
				var y = (float)cursorPosition.Y - (float)PointToScreen(new Point(0, 0)).Y;
				CurrentMouseY = y;

				if (Charts == null || ChartCount <= 0)
				{
					return;
				}

				if (x < 0 || x >= ActualWidth - ActualWidth / ChartCount)
				{
					if (CurrentMouseX != -1358)
					{
						CurrentMouseX = -1358;
						InvalidateVisual();
					}
					return;
				}

				CurrentMouseX = x;
				InvalidateVisual();
			}
			catch
			{
			}
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			if (Charts == null || ChartCount <= 0)
			{
				return;
			}

			LiveActualWidth = (float)ActualWidth;
			LiveActualHeight = (float)ActualHeight - CandleTopBottomMargin * 2;

			var canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.Transparent);

			var yMax = (double)Charts.Max(x => x.Quote.High);
			var yMin = (double)Charts.Min(x => x.Quote.Low);
			var vMax = (double)Charts.Max(x => x.Quote.Volume);
			var vMin = (double)Charts.Min(x => x.Quote.Volume);

			// Draw Quote and Indicator
			for (int i = 0; i < Charts.Count; i++)
			{
				var quote = Charts[i].Quote;

				#region Volume
				canvas.DrawRect(
					new SKRect(
						LiveActualItemFullWidth * i + LiveActualItemMargin / 2,
						LiveActualHeight * 0.66f + (float)(LiveActualHeight * 0.33f * (vMax - (double)quote.Volume) / vMax) + CandleTopBottomMargin,
						LiveActualItemFullWidth * (i + 1) - LiveActualItemMargin / 2,
						LiveActualHeight + CandleTopBottomMargin
						),
					quote.Open < quote.Close ? LongVolumePaint : ShortVolumePaint
					);
				#endregion

				#region Candle
				canvas.DrawLine(
					new SKPoint(
						LiveActualItemFullWidth * (i + 0.5f),
						LiveActualHeight * (float)(1.0 - ((double)quote.High - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
					new SKPoint(
						LiveActualItemFullWidth * (i + 0.5f),
						LiveActualHeight * (float)(1.0 - ((double)quote.Low - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
					quote.Open < quote.Close ? LongPaint : ShortPaint);
				canvas.DrawRect(
					new SKRect(
						LiveActualItemFullWidth * i + LiveActualItemMargin / 2,
						LiveActualHeight * (float)(1.0 - ((double)quote.Open - yMin) / (yMax - yMin)) + CandleTopBottomMargin,
						LiveActualItemFullWidth * (i + 1) - LiveActualItemMargin / 2,
						LiveActualHeight * (float)(1.0 - ((double)quote.Close - yMin) / (yMax - yMin)) + CandleTopBottomMargin
						),
					quote.Open < quote.Close ? LongPaint : ShortPaint
					);
				#endregion
			}

			// Draw Pointer
			canvas.DrawRect(
				(int)(CurrentMouseX / LiveActualItemFullWidth) * LiveActualItemFullWidth,
				0,
				LiveActualItemFullWidth,
				(float)ActualHeight,
				CandlePointerPaint
				);

			// Draw Horizontal Line Pointer
			canvas.DrawLine(
				0, CurrentMouseY, (float)ActualWidth, CurrentMouseY, HorizontalLinePointerPaint
				);
			// Draw Horizontal Line Price
			var pointingPrice = ((decimal)(((CandleTopBottomMargin - CurrentMouseY) / LiveActualHeight + 1) * (yMax - yMin) + yMin));
			canvas.DrawText($"{pointingPrice}", 2, CurrentMouseY - 4, CandleInfoFont, CandleInfoPaint);

			// Draw Info Text
			try
			{
				var pointingChart = CurrentMouseX == -1358 ? Charts[ChartCount - 1] : Charts[(int)(CurrentMouseX / LiveActualItemFullWidth)];
				var changeText = pointingChart.Quote.Close >= pointingChart.Quote.Open ? $"+{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}" : $"{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}";
				canvas.DrawText($"{pointingChart.DateTime:yyyy-MM-dd HH:mm:ss}, O {pointingChart.Quote.Open} H {pointingChart.Quote.High} L {pointingChart.Quote.Low} C {pointingChart.Quote.Close} V {pointingChart.Quote.Volume}", 3, 10, CandleInfoFont, CandleInfoPaint);
			}
			catch
			{
			}
		}
	}
}
