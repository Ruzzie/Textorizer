namespace Textorizer.Html
{
    /// <summary>
    /// Representation of a Html Element that are relevant for converting html content.
    /// </summary>
    public enum HtmlElementType
    {
        /// None
        None,
        /// Invalid Html
        Invalid,
        /// Any other valid (SG/HT)ML Element.
        Other,
        /// script element
        Script,
        /// style  element
        Style,
        /// pre  element
        Pre,
        /// p  element
        P,
        /// ul  element
        Ul,
        /// ol  element
        Ol,
        /// li  element
        Li,
        /// br  element
        Br,
        /// hr  element
        Hr,
    }
}