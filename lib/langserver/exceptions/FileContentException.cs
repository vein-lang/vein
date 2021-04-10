namespace wave.langserver.exceptions
{
    using System;

    public class FileContentException : Exception
    {
        /// <summary>
        /// Creates a <see cref="FileContentException"/> with the given message.
        /// </summary>
        public FileContentException(string message)
            : base(message)
        {
        }
    }
}