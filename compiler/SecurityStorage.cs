namespace vein;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DeviceId;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;

public class SecurityStorage
{
    public static readonly DirectoryInfo RootFolder
        = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein"));

    public static readonly FileInfo ConfigFile =
        RootFolder.File("vcfg");


    public static T GetByKey<T>(string key)
    {
        if (!HasKey(key))
            return default;
        return ReadStorage()[key].ToObject<T>();
    }

    public static bool HasKey(string key)
        => ReadStorage().ContainsKey(key);

    public static List<string> GetAllKeys()
        => ReadStorage().Select(x => x.Key).ToList();

    public static void AddKey<T>(string key, T value)
    {
        var store = ReadStorage();

        store[key] = JToken.FromObject(value);

        Save(store);
    }


    public static void RemoveKey(string key)
    {
        var store = ReadStorage();

        if (store.ContainsKey(key))
        {
            store.Remove(key);
            Save(store);
        }
    }

    private static Dictionary<string, JToken> ReadStorage()
    {
        try
        {
            if (!ConfigFile.Exists)
                return new Dictionary<string, JToken>();
            var content = ConfigFile.ReadToEnd();
            return JsonConvert.DeserializeObject<Dictionary<string, JToken>>(BlowfishDecrypt(content));
        }
        catch
        {
            if (ConfigFile.Exists)
                ConfigFile.Delete();
            return new Dictionary<string, JToken>();
        }
    }

    private static void Save(Dictionary<string, JToken> dict)
    {
        try
        {
            if (!RootFolder.Exists)
                RootFolder.Create();
            if (ConfigFile.Exists)
                ConfigFile.Delete();
            var content = JsonConvert.SerializeObject(dict);
            File.WriteAllText(ConfigFile.FullName, BlowfishEncrypt(content));
        }
        catch
        {
            if (ConfigFile.Exists)
                ConfigFile.Delete();
        }
    }

    private static string BlowfishEncrypt(string strValue)
    {
        try
        {
            BlowfishEngine engine = new BlowfishEngine();

            PaddedBufferedBlockCipher cipher = new PaddedBufferedBlockCipher(engine);

            KeyParameter keyBytes = new KeyParameter(Encoding.UTF8.GetBytes(__key__));

            cipher.Init(true, keyBytes);

            byte[] inB = Encoding.UTF8.GetBytes(strValue);

            byte[] outB = new byte[cipher.GetOutputSize(inB.Length)];

            int len1 = cipher.ProcessBytes(inB, 0, inB.Length, outB, 0);

            cipher.DoFinal(outB, len1);

            return BitConverter.ToString(outB).Replace("-", "");
        }
        catch
        {
            return "";
        }
    }

    private static string BlowfishDecrypt(string value)
    {
        BlowfishEngine engine = new BlowfishEngine();
        PaddedBufferedBlockCipher cipher = new PaddedBufferedBlockCipher(engine);

        StringBuilder result = new StringBuilder();

        cipher.Init(false, new KeyParameter(Encoding.UTF8.GetBytes(__key__)));

        byte[] out1 = Hex.Decode(value);
        byte[] out2 = new byte[cipher.GetOutputSize(out1.Length)];

        int len2 = cipher.ProcessBytes(out1, 0, out1.Length, out2, 0);

        cipher.DoFinal(out2, len2);

        var s2 = BitConverter.ToString(out2);

        for (int i = 0; i < s2.Length; i++)
        {
            char c = s2[i];
            if (c != 0)
            {
                result.Append(c.ToString());
            }
        }

        return Encoding.UTF8.GetString(result.ToString().Split('-').Select(x => byte.Parse(x, NumberStyles.AllowHexSpecifier))
            .ToArray());

    }

    private static string __key__;

    static SecurityStorage()
        => __key__ = CreateHardwareKey();

    private static string CreateHardwareKey() =>
        new DeviceIdBuilder()
            .AddMachineName()
            .AddOsVersion()
            .OnWindows(windows => windows
                .AddMotherboardSerialNumber()
                .AddMachineGuid()
                .AddProcessorId())
            .OnLinux(linux => linux
                .AddMotherboardSerialNumber()
                .AddCpuInfo())
            .OnMac(mac => mac
                .AddSystemDriveSerialNumber()
                .AddPlatformSerialNumber())
            .ToString();
}
