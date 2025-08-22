// File: SumMatrixTools.cs
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sum.Mcp.Server.Capabilities
{
    /// <summary>
    /// Generic demo tools for developers to see how to expose simple functions via MCP.
    /// No vendor-specific logic or external dependencies.
    ///
    /// Registration (example):
    ///   // during server startup, depending on your MCP host:
    ///   // services.AddMcpServerTools<SumMatrixTools>();
    ///
    /// Every public static method decorated with [McpServerTool] becomes callable.
    /// </summary>
    [McpServerToolType]
    public static class SumMatrixTools
    {
        // 1) Hello
        [McpServerTool(Name = "sum.hello"), Description("Returns a friendly greeting.")]
        public static object Hello(
            [Description("Optional name to personalize the greeting.")] string? name = null,
            CancellationToken ct = default)
        {
            var who = string.IsNullOrWhiteSpace(name) ? "Sum Matrix" : name.Trim();
            return new { message = $"Hello {who}!" };
        }

        // 2) Echo
        [McpServerTool(Name = "sum.echo"), Description("Echoes back the provided text.")]
        public static object Echo(
            [Description("Any text to echo back.")] string text,
            CancellationToken ct = default)
        {
            text ??= string.Empty;
            return new { text };
        }

        // 3) Time: now
        [McpServerTool(Name = "sum.time.now"), Description("Returns current time in UTC and an optional IANA timezone.")]
        public static object Now(
            [Description("IANA timezone like 'Europe/London' or 'Asia/Jerusalem'. If omitted, only UTC is returned.")]
            string? timezone = null,
            CancellationToken ct = default)
        {
            var utc = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(timezone))
            {
                return new
                {
                    utcIso = utc.ToString("o"),
                    unixSeconds = utc.ToUnixTimeSeconds()
                };
            }

            try
            {
#if NET6_0_OR_GREATER
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone.Trim());
                var local = TimeZoneInfo.ConvertTime(utc.UtcDateTime, tz);
                return new
                {
                    utcIso = utc.ToString("o"),
                    localIso = local.ToString("o"),
                    timezone = tz.Id,
                    unixSeconds = utc.ToUnixTimeSeconds()
                };
#else
                return new { error = "Timezone lookup not supported on this runtime.", utcIso = utc.ToString("o") };
#endif
            }
            catch (Exception ex)
            {
                return new { error = "Invalid timezone id.", message = ex.Message, utcIso = utc.ToString("o") };
            }
        }

        // 4) Time: parse → ISO / unix
        [McpServerTool(Name = "sum.time.parse"), Description("Parses a date/time input into ISO-8601 and Unix epoch seconds.")]
        public static object ParseTime(
            [Description("Date/time string. Accepts ISO-8601 or common formats, e.g. '2025-08-22T09:30:00Z' or '2025-08-22 09:30'.")]
            string input,
            [Description("Optional IANA timezone to interpret naive (no-Z) times.")]
            string? timezone = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new { error = "Empty input." };

            try
            {
                DateTime parsed;
                if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt1))
                {
                    parsed = dt1;
                }
                else if (DateTime.TryParse(input, out var dt2))
                {
                    parsed = dt2;
                }
                else
                {
                    return new { error = "Unrecognized date/time format." };
                }

#if NET6_0_OR_GREATER
                if (!string.IsNullOrWhiteSpace(timezone) && parsed.Kind == DateTimeKind.Unspecified)
                {
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone.Trim());
                        parsed = TimeZoneInfo.ConvertTimeToUtc(parsed, tz);
                    }
                    catch (Exception ex)
                    {
                        return new { error = "Invalid timezone id.", message = ex.Message };
                    }
                }
                else
                {
                    parsed = parsed.ToUniversalTime();
                }
#else
                parsed = parsed.ToUniversalTime();
#endif

                // Always output ISO 8601 with 'Z' for UTC
                var utc = parsed.ToUniversalTime();
                string iso = utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
                long unixSeconds = new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds();
                var dto = new DateTimeOffset(parsed, TimeSpan.Zero);

                return new { iso = dto.ToString("o"), unixSeconds = dto.ToUnixTimeSeconds() };
            }
            catch (Exception ex)
            {
                return new { error = "Parse failed.", message = ex.Message };
            }
        }

        // 5) UUID v4
        [McpServerTool(Name = "sum.uuid.new"), Description("Generates a new UUID (GUID).")]
        public static object NewUuid(CancellationToken ct = default)
            => new { uuid = Guid.NewGuid().ToString() };

        // 6) Math: add
        [McpServerTool(Name = "sum.math.add"), Description("Adds two numbers.")]
        public static object MathAdd(
            [Description("First number.")] double a,
            [Description("Second number.")] double b,
            CancellationToken ct = default)
            => new { result = a + b };

        // 7) Math: sum
        [McpServerTool(Name = "sum.math.sum"), Description("Sums an array of numbers.")]
        public static object MathSum(
            [Description("Array of numbers.")] double[] values,
            CancellationToken ct = default)
        {
            values ??= Array.Empty<double>();
            double s = 0;
            foreach (var v in values) s += v;
            return new { count = values.Length, sum = s };
        }

        // 8) Math: average
        [McpServerTool(Name = "sum.math.avg"), Description("Averages an array of numbers.")]
        public static object MathAvg(
            [Description("Array of numbers.")] double[] values,
            CancellationToken ct = default)
        {
            values ??= Array.Empty<double>();
            if (values.Length == 0) return new { count = 0, average = (double?)null };
            double s = 0;
            foreach (var v in values) s += v;
            return new { count = values.Length, average = s / values.Length };
        }

        // 9) Text: slugify
        [McpServerTool(Name = "sum.text.slugify"), Description("Converts text to a URL-friendly slug.")]
        public static object Slugify(
            [Description("Input text.")] string text,
            [Description("Replace spaces with this character. Default: '-'")] string? separator = "-",
            CancellationToken ct = default)
        {
            text ??= string.Empty;
            separator ??= "-";

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                    else if (char.IsWhiteSpace(ch) || ch == '_' || ch == '-' || ch == '.' || ch == '+') sb.Append(' ');
                }
            }

            var collapsed = string.Join(separator, sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            return new { slug = collapsed };
        }

        // 10) Text: extract emails
        [McpServerTool(Name = "sum.text.extract_emails"), Description("Extracts email-like tokens from text.")]
        public static object ExtractEmails(
            [Description("Text to scan.")] string text,
            CancellationToken ct = default)
        {
            text ??= string.Empty;
            var pattern = @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}";
            var emails = Regex.Matches(text, pattern).Cast<Match>().Select(m => m.Value).Distinct().ToArray();
            return new { count = emails.Length, emails };
        }

        // 11) JSON: validate & pretty
        [McpServerTool(Name = "sum.json.validate"), Description("Validates JSON and returns pretty-printed output if valid.")]
        public static object JsonValidate(
            [Description("JSON string.")] string json,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new { valid = false, error = "Empty input." };

            try
            {
                using var doc = JsonDocument.Parse(json);
                var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                return new { valid = true, pretty };
            }
            catch (Exception ex)
            {
                return new { valid = false, error = ex.Message };
            }
        }
    }
}
