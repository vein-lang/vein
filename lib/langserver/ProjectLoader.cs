namespace wave.langserver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    internal class ProjectLoader
    {
        public readonly Action<string, MessageType> Log;

        public ProjectLoader(Action<string, MessageType>? log = null) =>
            this.Log = log ?? ((_, __) => { });
        
        /// <summary>
        /// Returns a 1-way hash of the project file name so it can be sent with telemetry.
        /// if any exception is thrown, it just logs the error message and returns an empty string.
        /// </summary>
        internal string GetProjectNameHash(string projectFile)
        {
            try
            {
                using var hashAlgorithm = SHA256.Create();
                var fileName = Path.GetFileName(projectFile);
                var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(fileName));
                var sBuilder = new StringBuilder();
                foreach (var t in data) sBuilder.Append($"{t:X2}");
                return sBuilder.ToString();
            }
            catch (Exception e)
            {
                this.Log($"Error creating hash for project name '{projectFile}': {e.Message}", MessageType.Warning);
                return string.Empty;
            }
        }
    }
}