namespace Textorizer.Html
{
    /// <summary>
    /// Type of token in the source input text, used for scanning / lexing / parsing source input.
    /// </summary>
    public enum TokenType
    {
        ///Text content
        Text,
        /// html entity
        /// <code> &amp;lt; or &amp;#x160; </code>
        HtmlEntity,
        /// a html open tag
        /// <code>  &lt;p&gt; </code>
        HtmlOpenTag,
        /// a html open tag
        /// <code>  &lt;/p&gt; </code>
        HtmlCloseTag,
        /// a html open tag
        /// <code>  &lt;br/&gt; </code>
        HtmlSelfClosingTag,
        /// End of file / input
        Eof
    }
}