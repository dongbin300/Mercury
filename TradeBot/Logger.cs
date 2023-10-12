using Mercury;

using TradeBot.Models;

using System;
using System.IO;

namespace TradeBot
{
    public class Logger
    {
        public static void Log(string className, string? methodName, Exception exception)
        {
            Log(className, methodName, exception.ToString());
        }

        public static void Log(string className, string? methodName, string message)
        {
            File.AppendAllText($"Logs/{DateTime.Today:yyyyMMdd}.log", $"{DateTime.Now:HH:mm:ss.fff} [{className}.{methodName}] {message}" + Environment.NewLine);
        }

        public static void LogHistory(BotHistory botHistory)
        {
            File.AppendAllText($"Logs/{DateTime.Today:yyyyMMdd}_history.log", $"[{botHistory.DateTime:HH:mm:ss.fff}] [{botHistory.Subject}] {botHistory.Text}" + Environment.NewLine);
        }

        public static void LogReport(double estimatedBalance, double bnb, double todayPnl, decimal baseOrderSize, int leverage, int maxActiveDeals)
        {
            File.AppendAllText($"Logs/bot_report.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {estimatedBalance.Round(3)} USDT, {bnb.Round(3)} BNB, {(todayPnl >= 0 ? "+" : "")}{todayPnl.Round(3)} USDT, SIZE {baseOrderSize.Round(3)}, LEV {leverage}, MAX {maxActiveDeals}" + Environment.NewLine);
        }
    }
}
