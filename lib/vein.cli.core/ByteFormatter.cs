namespace vein;

public static class ByteFormatter
{
    public static string FormatBytes(this long bytes)
    {
        if (bytes < 0) return "0 bytes";
        if (bytes < 1024) return $"{bytes} bytes";
        if (bytes < 1048576) return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1073741824) return $"{bytes / 1048576.0:F2} MB";
        return $"{bytes / 1073741824.0:F2} GB";
    }
    public static string FormatBytesPerSecond(this long bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond} B/s";
        if (bytesPerSecond < 1048576)
            return $"{bytesPerSecond / 1024.0:F2} KB/s";
        if (bytesPerSecond < 1073741824)
            return $"{bytesPerSecond / 1048576.0:F2} MB/s";
        return $"{bytesPerSecond / 1073741824.0:F2} GB/s";
    }
    public static string FormatBytes(this int bytes)
    {
        if (bytes < 0) return "0 bytes";
        if (bytes < 1024) return $"{bytes} bytes";
        if (bytes < 1048576) return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1073741824) return $"{bytes / 1048576.0:F2} MB";
        return $"{bytes / 1073741824.0:F2} GB";
    }
    public static string FormatBytesPerSecond(this int bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond} B/s";
        if (bytesPerSecond < 1048576)
            return $"{bytesPerSecond / 1024.0:F2} KB/s";
        if (bytesPerSecond < 1073741824)
            return $"{bytesPerSecond / 1048576.0:F2} MB/s";
        return $"{bytesPerSecond / 1073741824.0:F2} GB/s";
    }
}
