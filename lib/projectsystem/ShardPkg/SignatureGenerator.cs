namespace vein.project.shards;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PgpCore;

public class SignatureGenerator
{
    public static readonly byte[] SIGN_BEGIN_PATTERN = new byte[29]
    {
        45, 45, 45, 45, 45,
        66, 69, 71, 73, 78, 32, 80, 71, 80, 32, 83, 73, 71, 78, 65, 84, 85, 82, 69,
        45, 45, 45, 45, 45
    };
    public static readonly byte[] SIGN_END_PATTERN = new byte[27]
    {
        45, 45, 45, 45, 45,
        69, 78, 68, 32, 80, 71, 80, 32, 83, 73, 71, 78, 65, 84, 85, 82, 69,
        45, 45, 45, 45, 45
    };
    public static readonly byte[] SIGN_MSG_BEGIN_PATTERN = new byte[]
    {
        45, 45, 45, 45, 45,
        66, 69, 71, 73, 78, 32, 80, 71, 80, 32, 83, 73, 71, 78, 69, 68, 32, 77, 69, 83, 83, 65, 71, 69,
        45, 45, 45, 45, 45
    };

    public static readonly byte[] HASH_PATTERN = new byte[]
    {
        72, 97, 115, 104, 58, 32, 83, 72, 65, 49
    };

    public static async Task<string> CreateAsync(EncryptionKeys key, Stream targetStream)
    {
        using var pgp = new PGP(key);
        using var mem = new MemoryStream();

        await pgp.ClearSignStreamAsync(targetStream, mem);
        var arr = mem.ToArray();

        var index = arr.AsSpan().LastIndexOf(SIGN_BEGIN_PATTERN);


        if (index == -1)
            return "";
        var text = Encoding.ASCII.GetString(arr.Skip(index).ToArray());


        return ExtractSign(text);
    }

    public static async Task<bool> Verify(EncryptionKeys key, Stream targetFile, string sign)
    {
        var line_end = Encoding.ASCII.GetBytes(Environment.NewLine);
        targetFile.Seek(0, SeekOrigin.Begin);
        using var pgp = new PGP(key);
        using var mem = new MemoryStream();

        await mem.WriteAsync(SIGN_MSG_BEGIN_PATTERN);
        await mem.WriteAsync(line_end);
        await mem.WriteAsync(HASH_PATTERN);
        await mem.WriteAsync(line_end);
        await mem.WriteAsync(line_end);
        await targetFile.CopyToAsync(mem);
        await mem.WriteAsync(SIGN_BEGIN_PATTERN);
        await mem.WriteAsync(line_end);
        await mem.WriteAsync(Encoding.ASCII.GetBytes("Version: BCPG C# v1.9.0.0"));
        await mem.WriteAsync(line_end);

        var sign_bytes = Encoding.ASCII.GetBytes(sign);

        await mem.WriteAsync(sign_bytes);
        await mem.WriteAsync(SIGN_END_PATTERN);
        await mem.WriteAsync(line_end);

        mem.Seek(0, SeekOrigin.Begin);
        return await pgp.VerifyClearStreamAsync(mem);
    }


    private static string ExtractSign(string text)
    {
        var str = new StringBuilder();
        foreach (string s in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(s))
                continue;
            if (s.StartsWith("-----"))
                continue;
            if (s.StartsWith("Version:"))
                continue;
            if (s.StartsWith("Comment:"))
                continue;
            str.Append(s.Replace("\r", ""));
            str.AppendLine();
        }

        return str.ToString();
    }
}
