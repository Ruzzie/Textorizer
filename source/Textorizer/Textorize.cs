using Textorizer.Html;

namespace Textorizer
{
    /// <summary>
    /// The main Textorize class.
    /// Contains all methods for performing string conversion functions.
    /// </summary>
    public static class Textorize
    {
        private static readonly HtmlTextorizer HtmlToPlainTextorizer = new HtmlTextorizer(new HtmlToPlainTextWriter());

        /// <summary>
        /// Converts html input to a safe plain text representation without html.
        /// </summary>
        /// <param name="html">the input string that contains html</param>
        /// <returns>a plain text representation of the input</returns>
        /// <remarks>
        /// Content in Style and Script tags are completely removed, html entity characters are explicitly converted to their unicode characters.
        /// Invalid html is handled best effort for a reasonable equivalent plain text output.
        /// <code>Textorize(input) == Textorize(HtmlEncode(Textorize(input)))</code>
        /// </remarks>
        public static string HtmlToPlainText(in string html)
        {
            return HtmlToPlainTextorizer.Textorize(html);
        }
    }
}