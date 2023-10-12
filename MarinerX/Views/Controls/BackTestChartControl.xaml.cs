using MarinerX.Bots;

using Mercury;

using MercuryTradingModel.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MarinerX.Views.Controls
{
    /// <summary>
    /// BackTestChartControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BackTestChartControl : UserControl
    {
        public List<Quote> Quotes = new();
        public List<BackTestTrade> Trades = new();

        private Pen yangPen = new(new SolidColorBrush(Color.FromRgb(59, 207, 134)), 1.0);
        private Pen yinPen = new(new SolidColorBrush(Color.FromRgb(237, 49, 97)), 1.0);
        private Brush yangBrush = new SolidColorBrush(Color.FromRgb(59, 207, 134));
        private Brush yinBrush = new SolidColorBrush(Color.FromRgb(237, 49, 97));
        private int candleMargin = 1;

        public int Start = 0;
        public int End = 0;
        public int ViewCountMin = 10;
        public int ViewCountMax = 1000;
        public int ViewCount => End - Start;
        public int TotalCount = 0;
        public Point CurrentMousePosition;

        public BackTestChartControl()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var itemWidth = ActualWidth / (ViewCount - 1);
            var max = Quotes.Skip(Start).Take(ViewCount).Max(x => x.High);
            var min = Quotes.Skip(Start).Take(ViewCount).Min(x => x.Low);

            for (int i = Start; i < End - 1; i++)
            {
                var quote = Quotes[i];
                var viewIndex = i - Start;

                // Draw Candle
                drawingContext.DrawLine(
                    quote.Open < quote.Close ? yangPen : yinPen,
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.High - min) / (max - min))),
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.Low - min) / (max - min))));
                drawingContext.DrawRectangle(
                    quote.Open < quote.Close ? yangBrush : yinBrush,
                    quote.Open < quote.Close ? yangPen : yinPen,
                    new Rect(
                    new Point(itemWidth * viewIndex + candleMargin, ActualHeight * (double)(1.0m - (quote.Open - min) / (max - min))),
                    new Point(itemWidth * (viewIndex + 1) - candleMargin, ActualHeight * (double)(1.0m - (quote.Close - min) / (max - min)))
                    ));

                // Draw Buy/Sell History Arrow
                var trade = Trades.Find(x => x.time.Equals(quote.Date));
                if (trade != null)
                {
                    if (trade.side == PositionSide.Long)
                    {
                        drawingContext.DrawLine(yangPen,
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.Low - min) / (max - min)) + 24),
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.Low - min) / (max - min)) + 10));
                        drawingContext.DrawLine(yangPen,
                    new Point(itemWidth * (viewIndex + 0.25), ActualHeight * (double)(1.0m - (quote.Low - min) / (max - min)) + 15),
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.Low - min) / (max - min)) + 10));
                        drawingContext.DrawLine(yangPen,
                    new Point(itemWidth * (viewIndex + 0.75), ActualHeight * (double)(1.0m - (quote.Low - min) / (max - min)) + 15),
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.Low - min) / (max - min)) + 10));
                    }
                    else
                    {
                        drawingContext.DrawLine(yinPen,
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.High - min) / (max - min)) - 24),
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.High - min) / (max - min)) - 10));
                        drawingContext.DrawLine(yinPen,
                    new Point(itemWidth * (viewIndex + 0.25), ActualHeight * (double)(1.0m - (quote.High - min) / (max - min)) - 15),
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.High - min) / (max - min)) - 10));
                        drawingContext.DrawLine(yinPen,
                    new Point(itemWidth * (viewIndex + 0.75), ActualHeight * (double)(1.0m - (quote.High - min) / (max - min)) - 15),
                    new Point(itemWidth * (viewIndex + 0.5), ActualHeight * (double)(1.0m - (quote.High - min) / (max - min)) - 10));
                    }
                }
            }
        }

        private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scaleUnit = Math.Max(1, ViewCount * Math.Abs(e.Delta) / 2000);
            if (e.Delta > 0) // Zoom-in
            {
                if (ViewCount <= ViewCountMin)
                {
                    return;
                }

                Start = Math.Min(TotalCount - ViewCountMin, Start + scaleUnit);
                End = Math.Max(ViewCountMin, End - scaleUnit);
            }
            else // Zoom-out
            {
                if (ViewCount >= ViewCountMax)
                {
                    return;
                }

                Start = Math.Max(0, Start - scaleUnit);
                End = Math.Min(TotalCount, End + scaleUnit);
            }

            InvalidateVisual();
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            var itemWidth = ActualWidth / (ViewCount - 1);
            Vector diff = e.GetPosition(Parent as Window) - CurrentMousePosition;
            if (IsMouseCaptured)
            {
                var moveUnit = (int)(diff.X / itemWidth / 30);
                if (diff.X > 0) // Graph Move Left
                {
                    if (Start > moveUnit)
                    {
                        Start -= moveUnit;
                        End -= moveUnit;
                        InvalidateVisual();
                    }
                }
                else if (diff.X < 0) // Graph Move Right
                {
                    if (End < TotalCount + moveUnit)
                    {
                        Start -= moveUnit;
                        End -= moveUnit;
                        InvalidateVisual();
                    }
                }
            }
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentMousePosition = e.GetPosition(Parent as Window);
            CaptureMouse();
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
        }
    }
}
