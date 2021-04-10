namespace wave.langserver.exceptions
{
    using System;

    public class SymbolNotFoundException : Exception
    {
        /// <summary>
        /// Creates a <see cref="FileContentException"/> with the given message.
        /// </summary>
        public SymbolNotFoundException(string message)
            : base(message)
        {
        }
    }
}