namespace wave.fs
{
    using System.Collections.Generic;
    using System.IO;
    using runtime;
    using static System.Environment;
    using static System.Environment.SpecialFolder;
    public static class Insomnia
    {
        public static DirectoryInfo SDKPath => 
            new (Path.Combine(GetFolderPath(CommonProgramFilesX86), "WaveLang", "sdk", "0.1-preview"));
        
        public static List<WaveModule> LoadSDK()
        {
            IEnumerable<FileInfo> libs() =>
                SDKPath.EnumerateFiles("*.wll", SearchOption.AllDirectories);
            
            return default;
        }
    }
}