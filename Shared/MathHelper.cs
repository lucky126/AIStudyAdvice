using System.Text.RegularExpressions;

namespace Study.Shared
{
    public static class MathHelper
    {
        public static string FixMathInHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;

            // Simple regex to iterate over text nodes (anything not inside <...>)
            // We use a pattern that matches tags or text
            return Regex.Replace(html, @"(<[^>]+>)|([^<]+)", m => {
                // If it's a tag, return as is
                if (m.Groups[1].Success) return m.Value;
                
                // If it's text, process it
                return FixMathContent(m.Value, isHtml: true);
            });
        }

        public static string FixMathContent(string input, bool isHtml = false)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // If already has delimiters, assume it's well-formed and don't touch it
            // We check for standard LaTeX delimiters: $, \(, \[
            if (input.Contains("$") || input.Contains("\\(") || input.Contains("\\["))
                return input;

            // Check if it contains math-like characters that suggest it needs formatting
            // We look for backslashes (LaTeX commands) or common math operators
            // For HTML, we also look for encoded entities like &lt; &gt;
            bool hasMath = input.Contains("\\") || input.Contains("<") || input.Contains(">") || 
                          input.Contains("=") || input.Contains("^") ||
                          (isHtml && (input.Contains("&lt;") || input.Contains("&gt;")));
            
            if (!hasMath) return input;

            // Regex to match potential math segments
            // We match sequences that are NOT:
            // - CJK Unified Ideographs (4E00-9FFF)
            // - CJK Symbols and Punctuation (3000-303F)
            // - Fullwidth Forms (FF00-FFEF) (e.g. ，：？)
            // - Newlines
            // This allows ASCII, Greek, Latin-1 Supplement (like ²), etc. to be grouped as math.
            return Regex.Replace(input, @"([^\u4e00-\u9fff\u3000-\u303f\uff00-\uffef\r\n]+)", m => {
                var val = m.Value;
                if (string.IsNullOrWhiteSpace(val)) return val;
                
                // Extract leading and trailing whitespace to preserve them outside the math delimiter
                var leading = new string(val.TakeWhile(char.IsWhiteSpace).ToArray());
                var trailing = new string(val.Reverse().TakeWhile(char.IsWhiteSpace).Reverse().ToArray());
                
                // Get the core content
                var middle = val.Trim();
                if (string.IsNullOrEmpty(middle)) return val;

                // Heuristic: If it looks like plain English text (no numbers, no symbols), skip it.
                // Math usually has numbers, operators, or single letters (variables).
                // If it's longer than 2 words and only contains letters/spaces/punctuation, likely text.
                bool isPlainText = !Regex.IsMatch(middle, @"[0-9\+\-\*\/=\<\>\^\\\&]") && 
                                   Regex.IsMatch(middle, @"^[a-zA-Z\s\.,;:!?\(\)]+$");
                
                // Refined check: single letters like 'm' or 'x' ARE math.
                // So "isPlainText" should apply if length > 1 and looks like a word?
                if (isPlainText && middle.Length > 2 && middle.Contains(" "))
                {
                     return val;
                }

                // Check for list markers like "1." or "a)" or simple numbers
                if (Regex.IsMatch(middle, @"^(\d+\.?|[a-zA-Z]\)|[0-9]+)$"))
                {
                    return val;
                }

                // Fix common unicode superscripts/symbols that might break standard MathJax or look wrong
                middle = middle.Replace("²", "^2").Replace("³", "^3");

                return $"{leading}\\({middle}\\){trailing}";
            });
        }
    }
}
