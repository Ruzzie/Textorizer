namespace Textorizer
{
    /// <summary>
    /// Interface for Textorizer implementations.
    /// A Textorizer converts a given input to a implementation specific plain text representation.
    /// </summary>
    internal interface ITextorizer
    {
        /// <summary>
        /// Converts an input format to another. Used for sanitization, stripping or converting an input format
        /// to another format
        /// </summary>
        /// <param name="textToSanitize">the input text</param>
        /// <returns>the converted input</returns>
        string Textorize(in string textToSanitize);
    }
}