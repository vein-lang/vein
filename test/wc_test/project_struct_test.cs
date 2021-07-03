namespace wc_test
{
    using Newtonsoft.Json;
    using mana.project;
    using NUnit.Framework;

    public class project_struct_test
    {
        [Test]
        public void LoadSDKJson()
        {
            var text = "{\r\n  \"version\": \"1.0.0\",\r\n  \"name\": \"default\",\r\n  \"workloads\": {\r\n      \"wave.sdk.unity\": {\r\n          \"description\": \"Mana SDK\",\r\n          \"packs\": [\r\n            \"Ishtar.Windows.Sdk\",\r\n            \"Ishtar.OSX.Sdk\",\r\n            \"Ishtar.Linux.Sdk\"\r\n          ]\r\n      }\r\n  },\r\n  \"packs\":{\r\n      \"Ishtar.Windows.Sdk\": {\r\n          \"kind\": \"Sdk\",\r\n          \"version\": \"1.0\",\r\n          \"alias\": \"win10-x64\"\r\n      },\r\n      \"Ishtar.OSX.Sdk\": {\r\n          \"kind\": \"Sdk\",\r\n          \"version\": \"1.0\",\r\n          \"alias\": \"osx10.5-x64\"\r\n      },\r\n      \"Ishtar.Linux.Sdk\": {\r\n          \"kind\": \"Sdk\",\r\n          \"version\": \"1.0\",\r\n          \"alias\": \"linux-x64\"\r\n      }\r\n  }\r\n}";
            var result = JsonConvert.DeserializeObject<ManaSDK>(text);

            Assert.AreEqual(3, result.Packs.Length);
        }
    }
}
