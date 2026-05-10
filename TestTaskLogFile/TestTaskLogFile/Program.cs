using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LogAnalyzer
{
    enum LogLevel
    {
        INFO,
        WARNING,
        ERROR,
        CRITICAL
    }

    class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
    }

    class LogParser
    {
        const int DateLength = 19; // "yyyy-MM-dd HH:mm:ss" is 19 characters
        public static LogEntry? Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            try
            {
                // [2000-01-01 00:00:00] [ERROR] Message example
                int firstClose = line.IndexOf(']');
                int secondOpen = line.IndexOf('[', firstClose);
                int secondClose = line.IndexOf(']', secondOpen);

                if (firstClose == -1 || secondOpen == -1 || secondClose == -1)
                    return null;

                string datePart = line.Substring(1, firstClose - 1);
                string levelPart = line.Substring(secondOpen + 1, secondClose - secondOpen - 1);

                if (datePart.Length != DateLength) 
                    return null;

                if (!DateTime.TryParse(datePart, out var timestamp))
                    return null;

                if (!Enum.TryParse<LogLevel>(levelPart, true, out var level))
                    return null;

                return new LogEntry
                {
                    Timestamp = timestamp,
                    Level = level
                };
            }
            catch
            {
                return null;
            }
        }
    }

    class LogAnalyzer
    {
        public static async Task AnalyzeAsync(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found.");
                return;
            }

            var stats = new Dictionary<DateTime, int>();

            using (var reader = new StreamReader(path))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var entry = LogParser.Parse(line);

                    if (entry != null && (entry.Level == LogLevel.ERROR || entry.Level == LogLevel.CRITICAL))
                    {
                        var hour = new DateTime(entry.Timestamp.Year, entry.Timestamp.Month, entry.Timestamp.Day, entry.Timestamp.Hour, 0, 0);

                        if (stats.ContainsKey(hour))
                            stats[hour]++;
                        else
                            stats[hour] = 1;
                    }
                }
            }

            var report = stats
                .OrderBy(s => s.Key)
                .Select(s => new { Hour = s.Key, Count = s.Value });

            foreach (var item in report)
            {
                Console.WriteLine($"{item.Hour:yyyy-MM-dd HH:mm} → {item.Count} [ERROR/CRITICAL]");
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {

            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //string path = Path.GetFullPath(Path.Combine(exeDirectory, @"..\..\..\Logs_Test\logs_Test_2.txt")); //Heavy load test
            string path = Path.GetFullPath(Path.Combine(exeDirectory, @"..\..\..\Logs_Test\logs_Test_1.txt"));

            await LogAnalyzer.AnalyzeAsync(path);

            Console.WriteLine("Complete.");
        }
    }
}