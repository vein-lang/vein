namespace ishtar;
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

[ExcludeFromCodeCoverage]
internal class NativeProviderLoader
{
    private static readonly object guarder = new object();


    const string X86 = "x86";
    const string X64 = "x64";
    const string IA64 = "ia64";
    const string ARM = "arm";
    const string ARM64 = "arm64";

    /// <summary>
    /// Dictionary of handles to previously loaded libraries,
    /// </summary>
    static readonly Lazy<Dictionary<string, IntPtr>> NativeHandles = new(LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Gets a string indicating the architecture and bitness of the current process.
    /// </summary>
    static readonly Lazy<string> ArchitectureKey = new(EvaluateArchitectureKey, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// If the last native library failed to load then gets the corresponding exception
    /// which occurred or null if the library was successfully loaded.
    /// </summary>
    internal static Exception? LastException { get; private set; }


    static bool IsUnix =>
        Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX;


    static string EvaluateArchitectureKey()
    {
        //return (IntPtr.Size == 8) ? X64 : X86;
        if (IsUnix) // Only support x86 and amd64 on Unix as there isn't a reliable way to detect the architecture
            return Environment.Is64BitProcess ? X64 : X86;

        var architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "unknown";

        if (string.Equals(architecture, "x86", StringComparison.OrdinalIgnoreCase))
            return X86;

        if (string.Equals(architecture, "amd64", StringComparison.OrdinalIgnoreCase)
            || string.Equals(architecture, "x64", StringComparison.OrdinalIgnoreCase))
            return Environment.Is64BitProcess ? X64 : X86;

        if (string.Equals(architecture, "ia64", StringComparison.OrdinalIgnoreCase))
            return IA64;

        if (string.Equals(architecture, "arm", StringComparison.OrdinalIgnoreCase))
            return Environment.Is64BitProcess ? ARM64 : ARM;

        // Fallback if unknown
        return architecture;
    }

    /// <summary>
    /// Load the native library with the given filename.
    /// </summary>
    /// <param name="fileName">The file name of the library to load.</param>
    /// <param name="hintPath">Hint path where to look for the native binaries. Can be null.</param>
    /// <returns>True if the library was successfully loaded or if it has already been loaded.</returns>
    internal static bool TryLoad(string fileName, string? hintPath)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        // If we have hint path provided by the user, look there first
        if (TryLoadFromDirectory(fileName, hintPath))
            return true;

        // If we have an overall hint path provided by the user, look there next
        //if (Control.NativeProviderPath != hintPath && TryLoadFromDirectory(fileName, Control.NativeProviderPath))
        //    return true;

        // Look under the current AppDomain's base directory
        if (TryLoadFromDirectory(fileName, AppDomain.CurrentDomain.BaseDirectory))
            return true;

        // Look at this assembly's directory
        if (TryLoadFromDirectory(fileName, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            return true;

        return false;
    }
    
    /// <summary>
    /// Try to load a native library by providing its name and a directory.
    /// Tries to load an implementation suitable for the current CPU architecture
    /// and process mode if there is a matching subfolder.
    /// </summary>
    /// <returns>True if the library was successfully loaded or if it has already been loaded.</returns>
    static bool TryLoadFromDirectory(string fileName, string? directory)
    {
        if (!Directory.Exists(directory))
            return false;

        directory = Path.GetFullPath(directory);

        // If we have a know architecture, try the matching subdirectory first
        var architecture = ArchitectureKey.Value;
        if (!string.IsNullOrEmpty(architecture) && TryLoadFile(new FileInfo(Path.Combine(Path.Combine(directory, architecture), fileName))))
            return true;

        // Otherwise try to load directly from the provided directory
        return TryLoadFile(new FileInfo(Path.Combine(directory, fileName)));
    }

    /// <summary>
    /// Try to load a native library by providing the full path including the file name of the library.
    /// </summary>
    /// <returns>True if the library was successfully loaded or if it has already been loaded.</returns>
    internal static bool TryLoadFile(FileInfo file)
    {
        lock (guarder)
        {
            if (NativeHandles.Value.TryGetValue(file.Name, out var libraryHandle))
                return true;

            // If the library isn't found within an architecture specific folder then return false
            // to allow normal P/Invoke searching behavior when the library is called
            if (!file.Exists)
                return false;

            // If successful this will return a handle to the library
            libraryHandle =
                IsUnix ? UnixLoader.LoadLibrary(file.FullName) : WindowsLoader.LoadLibrary(file.FullName);
            if (libraryHandle == IntPtr.Zero)
            {
                var lastError = Marshal.GetLastWin32Error();
                var exception = new System.ComponentModel.Win32Exception(lastError);
                LastException = exception;
            }
            else
            {
                LastException = null;
                NativeHandles.Value[file.Name] = libraryHandle;
            }

            return libraryHandle != IntPtr.Zero;
        }
    }

    /// <summary>
    /// Try to load a native library by providing the full path including the file name of the library.
    /// </summary>
    /// <returns>True if the library was successfully loaded or if it has already been loaded.</returns>
    internal static bool TryLoadFile(FileInfo file, out nint handle)
    {
        handle = IntPtr.Zero;
        if (TryLoadFile(file))
            return NativeHandles.Value.TryGetValue(file.Name, out handle);
        return false;
    }
}

[SuppressUnmanagedCodeSecurity]
[SecurityCritical]
[ExcludeFromCodeCoverage]
internal static class WindowsLoader
{
    public static IntPtr LoadLibrary(string fileName)
        => LoadLibraryEx(fileName, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);

    // Search for dependencies in the library's directory rather than the calling process's directory
    private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

    [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string fileName, IntPtr reservedNull, uint flags);
}

[SuppressUnmanagedCodeSecurity]
[SecurityCritical]
[ExcludeFromCodeCoverage]
internal static class UnixLoader
{
    public static IntPtr LoadLibrary(string fileName)
        => dlopen(fileName, RTLD_NOW);

    private const int RTLD_NOW = 2;

    [DllImport("libdl.so", SetLastError = true)]
    private static extern IntPtr dlopen(string fileName, int flags);
}
