using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace NLog.LayoutRenderers;

[LayoutRenderer("ColoredConsole")]
public class ColoredConsoleLayout : LayoutRenderer
{
    private const string AnsiReset = "\e[0m";
    private const string AnsiGray = "\e[38;5;8m";

    private static KeyValuePair<string, string>[] nameAndColors =
    [
        new("TRE", AnsiGray), // Trace
        new("DEG", "\e[37m"),
        new("INF", "\e[92m"),
        new("WRN", "\e[93m"),
        new("ERR", "\e[91m"),
        new("FAL", "\e[95m")
    ];

    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        if (logEvent.Level == LogLevel.Off)
            return;

        (string name, string color) = nameAndColors[logEvent.Level.Ordinal];
        builder.Append(color);
        builder.Append(name);
        builder.Append(AnsiReset);

        builder.Append(AnsiGray);
        builder.Append(" Thread-");
        builder.Append($"{Environment.CurrentManagedThreadId:000}");
        builder.Append(" [");
        builder.Append(AnsiReset);

        int padRight = 0;
        const int fixedLen = 30;
        int strLength = builder.Length;
        if (!string.IsNullOrEmpty(logEvent.LoggerName))
        {
            builder.Append(logEvent.LoggerName);
        }
        else
        {
            if (logEvent.CallerLineNumber != 0)
            {
                var fileName = Path.GetFileName(logEvent.CallerFilePath.AsSpan());
                builder.Append($"{fileName}:{logEvent.CallerLineNumber}");
            }
        }

        padRight = fixedLen - (builder.Length - strLength);
        for (int i = 0; i < padRight; i++)
            builder.Append(' ');

        builder.Append(AnsiGray);
        builder.Append(']');

        builder.Append(" - ");

        if (logEvent.Level == LogLevel.Info)
        {
            builder.Append(AnsiReset);
        }
        else
        {
            builder.Append(color);
        }

        builder.Append(logEvent.FormattedMessage);

        if (logEvent.Exception != null)
        {
            builder.AppendLine();
            AppendException(builder, logEvent.Exception);
        }

        builder.Append(AnsiReset);
    }

    private void AppendException(StringBuilder builder, Exception e)
    {
        string[] lines = e.ToString().Split("\r\n");
        foreach (var line in lines)
        {
            // 替换路径 是为支持在rider的控制台中 点击跳转
            string matchText = "(in .:.+:)line (\\d+)";
            Match match = Regex.Match(line, matchText);
            if (match.Success)
            {
                string replaceTxt = match.Groups[1].Value + match.Groups[2].Value;
                string result = Regex.Replace(line, matchText, replaceTxt);
                builder.AppendLine(result);
            }
            else
            {
                builder.AppendLine(line);
            }
        }
    }
}