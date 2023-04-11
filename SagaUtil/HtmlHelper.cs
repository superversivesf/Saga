using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace SagaUtil;

public static class HtmlHelper
{
    private static readonly Random rng = new();

    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        string buf;
        var block = "address|article|aside|blockquote|canvas|dd|div|dl|dt|" +
                    "fieldset|figcaption|figure|footer|form|h\\d|header|hr|li|main|nav|" +
                    "noscript|ol|output|p|pre|section|table|tfoot|ul|video";

        var patNestedBlock = $"(\\s*?</?({block})[^>]*?>)+\\s*";
        buf = Regex.Replace(html, patNestedBlock, "\n", RegexOptions.IgnoreCase);

        // Replace br tag to newline.
        buf = Regex.Replace(buf, @"<(br)[^>]*>", "\n", RegexOptions.IgnoreCase);

        // (Optional) remove styles and scripts.
        buf = Regex.Replace(buf, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.Singleline);

        // Remove all tags.
        buf = Regex.Replace(buf, @"<[^>]*(>|$)", "", RegexOptions.Multiline);

        // Replace HTML entities.
        buf = WebUtility.HtmlDecode(buf);
        return buf;
    }
}