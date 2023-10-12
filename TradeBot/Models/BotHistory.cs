using System;
using System.Windows.Media;

namespace TradeBot.Models
{
    public class BotHistory
    {
        public DateTime DateTime { get; set; }
        public string Time => DateTime.ToString("yyyy-MM-dd HH:mm:ss");
        public string Text { get; set; }
        public string Subject { get; set; }
        public SolidColorBrush TextColor => GetTextColor();

        public BotHistory(DateTime dateTime, string subject, string text)
        {
            DateTime = dateTime;
            Subject = subject;
            Text = text;
        }

        private SolidColorBrush GetTextColor()
        {
            if (Text.StartsWith("Take Profit"))
            {
                return Common.LongColor;
            }
            else if (Text.StartsWith("Stop Loss"))
            {
                return Common.ShortColor;
            }
            else
            {
                return Common.WhiteColor;
            }
        }
    }
}
